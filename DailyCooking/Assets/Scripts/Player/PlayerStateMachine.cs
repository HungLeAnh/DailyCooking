using CodeMonkey.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;

public class PlayerStateMachine : NetworkBehaviour, IKitchenObjectParent
{
    public PlayerStateContext Context { get; set; }
    public static PlayerStateMachine LocalInstance { get; private set; }
    // Every spawned avatar on this peer (e.g. for hiding avatars that block a close-up).
    private static readonly List<PlayerStateMachine> all = new List<PlayerStateMachine>();
    public static IReadOnlyList<PlayerStateMachine> All => all;

    public event EventHandler OnPickedSomething;
    public enum EPlayerState
    {
        Idle, 
        Walking,
    }


    [SerializeField]
    private LayerMask countersLayerMask;
    [SerializeField] 
    private Transform kitchenObjectHoldPoint;
    [SerializeField] 
    private Animator characterAnimator;
    [SerializeField] 
    private List<CustomizationPart> customizationParts;
    [SerializeField]
    private PlayerIKHandler playerIKHandler;


    [SerializeField] private float radius = 2f;
    private float height = 2.0f;
    private const int MAX_HIGHLIGHT_HITS = 32;
    private readonly Collider[] highlightHitBuffer = new Collider[MAX_HIGHLIGHT_HITS];
    private readonly List<IHighlightable> nearbyHighlightables = new List<IHighlightable>();
    private readonly List<(IHighlightable highlight, IInteractable interactable)> targetCandidates = new List<(IHighlightable, IInteractable)>();
    private readonly List<Vector3> targetCandidatePositions = new List<Vector3>();
    private IHighlightable highlightedTarget;
    // Slack on top of the highlight radius for movement lag between owner and server.
    private const float INTERACT_RANGE_TOLERANCE = 1.5f;
    // The owner's stats this avatar follows for its look (see SubscribeToOwnerStats).
    private PlayerStats subscribedOwnerStats;
    private GameData subscribedGameData;


    private StateManager<EPlayerState> _stateManager;
    private PlayerStateFactory _stateFactory;


    private void IntializeStates()
    {
        Context = new PlayerStateContext(characterAnimator,
            this.transform, countersLayerMask, this.kitchenObjectHoldPoint, playerIKHandler);

        var states = _stateFactory.CreateStates(this);
        _stateManager.SetStates(states, EPlayerState.Idle);
        _stateManager.Start();

        SetCharacterMesh();
    }

    // Shows the look of the player who owns this avatar (every peer renders every avatar).
    private void SetCharacterMesh()
    {
        foreach (var part in customizationParts)
        {
            part.Initialise(ConfigManager.Instance.CustomizationData);
        }

        if (!IsSpawned || GameManager.Instance?.GameData == null) return;

        var playerData = GetOwnerStats();
        if (playerData == null) return;
        foreach (var item in playerData.CharacterCustomizationIds)
        {
            var part = customizationParts.FirstOrDefault(x => x.Type == item.Key);
            if (part != null)
            {
                if (item.Value >= 0)
                    part.SetMesh(item.Value, false);
                else
                    part.Clear();
            }
        }

        foreach (var part in customizationParts)
        {
            part.Close();
        }
    }

    protected void Awake()
    {
        _stateManager = new StateManager<EPlayerState>();
        _stateFactory = new PlayerStateFactory();
        IntializeStates();

    }
    private void Start()
    {
        GameInput.Instance.OnInteract1Performed += GameInput_OnInteract1Performed;
    }

    private void OnResourceChanged()
    {
        SetCharacterMesh();
    }

    public override void OnDestroy()
    {
        if(GameInput.Instance != null)
            GameInput.Instance.OnInteract1Performed -= GameInput_OnInteract1Performed;

        Context = null;
        _stateManager.Dispose();
        base.OnDestroy();
    }
    private void Update()
    {
        if (!IsOwner) return;

        _stateManager.Update();

        HandleMovement();
        HandleInteractions();
    }

    public Transform GetKitchenObjectFollowTransform(int index = 0)
    {
        return kitchenObjectHoldPoint;
    }

    public void SetKitchenObject(KitchenObject kitchenObject, int index = 0)
    {
        Context.KitchenObject = kitchenObject;

        if (kitchenObject != null)
        {
            OnPickedSomething?.Invoke(this, EventArgs.Empty);
        }
    }

    public KitchenObject GetKitchenObject(int index = 0)
    {
        return Context.KitchenObject;
    }

    public void ClearKitchenObject(int index = 0)
    {
        Context.KitchenObject = null;
    }

    public bool HasKitchenObject(int index = 0)
    {
        return Context.KitchenObject != null;
    }
    public void DisableInput(bool isDisable)
    {
        Context.IsDisableInput = isDisable;
    }
    private void GameInput_OnInteract1Performed(object sender, EventArgs e)
    {
        PressInteract();
    }

    // Owner only: the Interact button (or F). Acts on Context.InteractTarget, picking the alternate
    // interaction for a target that is mid-progress (e.g. the next cut), as tapping it again used to.
    // Interacting with an installed cooking tool also glides the local camera into its close-up.
    public void PressInteract()
    {
        if (!IsOwner || Context == null || Context.IsDisableInput)
            return;
        // Building: the USE button is hidden; keyboard F must not act on the kitchen either.
        if (CameraModeController.Instance != null && CameraModeController.Instance.Mode == CameraMode.Build)
            return;
        IInteractable target = Context.InteractTarget;
        if (target == null)
            return;

        // At a cooking tool, USE only glides into the close-up; cooking itself is done there by
        // dragging and gestures, so nothing is sent to the server.
        if (target is CookingToolCounterController controller && controller.HasToolInstalled())
        {
            CameraModeController cameraMode = CameraModeController.Instance;
            if (cameraMode != null && cameraMode.Mode == CameraMode.Walk)
                cameraMode.RequestCloseUp(controller.InstalledTool);
            return;
        }

        bool alternate = target is IHasProgress progress && !progress.IsDone() && progress.GetProgress() != -1;
        RequestInteract(target, alternate);
    }

    private static IInteractable ResolveInteractable(Component component)
    {
        if (component == null)
            return null;
        IInteractable interactable = component.GetComponentInParent<IInteractable>();
        if (interactable != null)
            return interactable;
        // A stacked tool is not IInteractable itself: its counter's controller is.
        CookingTool tool = component.GetComponentInParent<CookingTool>();
        return tool != null ? CookingToolCounterController.FindControllerForTool(tool) : null;
    }

    private static Vector3 MovementDirection(Vector2 inputVector)
    {
        CameraModeController cameraMode = CameraModeController.Instance;
        float yaw = cameraMode != null && cameraMode.Mode == CameraMode.Walk ? cameraMode.WalkYaw : 0f;
        return CameraRelativeMovement.ToWorld(inputVector, yaw);
    }

    private static bool IsInCloseUp =>
        CameraModeController.Instance != null && CameraModeController.Instance.Mode == CameraMode.CloseUp;

    private void HandleInteractions()
    {
        if (!IsInCloseUp)
        {
            Vector3 moveDir = MovementDirection(GameInput.Instance.GetMovementVectorNormalized());
            if (moveDir != Vector3.zero)
                Context.LastInteractDir = moveDir;
        }

        Vector3 p1 = transform.position + Vector3.up * radius;
        Vector3 p2 = transform.position + Vector3.up * (height - radius);

        // Runs every frame: reuse buffers. Context.Highlightable stays the full in-range list
        // (CookingStartButtonWorldUI reads it); only the chosen target is visibly highlighted.
        int hitCount = Physics.OverlapCapsuleNonAlloc(p1, p2, radius, highlightHitBuffer, countersLayerMask);
        nearbyHighlightables.Clear();
        targetCandidates.Clear();
        targetCandidatePositions.Clear();
        for (int i = 0; i < hitCount; i++)
        {
            if (!highlightHitBuffer[i].transform.TryGetComponent(out IHighlightable highlightable) ||
                nearbyHighlightables.Contains(highlightable))
                continue;
            nearbyHighlightables.Add(highlightable);
            IInteractable interactable = ResolveInteractable(highlightable as Component);
            if (interactable is NetworkBehaviour behaviour && behaviour.IsSpawned)
            {
                targetCandidates.Add((highlightable, interactable));
                targetCandidatePositions.Add(highlightHitBuffer[i].transform.position);
            }
        }
        Context.Highlightable.Clear();
        Context.Highlightable.AddRange(nearbyHighlightables);

        IHighlightable targetHighlight = null;
        IInteractable target = null;
        CookingTool closeUpTool = IsInCloseUp ? CameraModeController.Instance.CloseUpTool : null;
        if (closeUpTool != null)
        {
            target = CookingToolCounterController.FindControllerForTool(closeUpTool);
            targetHighlight = target as IHighlightable;
        }
        else
        {
            int index = InteractTargetSelector.SelectIndex(transform.position, transform.forward, targetCandidatePositions);
            if (index >= 0)
                (targetHighlight, target) = targetCandidates[index];
        }
        Context.InteractTarget = target;

        if (!ReferenceEquals(targetHighlight, highlightedTarget))
        {
            if (highlightedTarget as UnityEngine.Object != null)
                highlightedTarget.OnDeselected();
            if (targetHighlight as UnityEngine.Object != null)
                targetHighlight.OnSelected();
            highlightedTarget = targetHighlight;
        }
    }
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position + Vector3.up * radius , radius);
        Gizmos.DrawWireSphere(transform.position + Vector3.up * (height - radius), radius);
    }
    private void HandleMovement()
    {
        if (GameManager.Instance?.GameData == null)
            return;

        var stats = GetOwnerStats();
        if (stats == null) return;

        // In a close-up the joystick does nothing; only the Back button leaves it.
        if (IsInCloseUp)
        {
            Context.IsWalking = false;
            return;
        }

        Vector2 inputVector = Context.PlayerGameInput.GetMovementVectorNormalized();

        Vector3 moveDir = MovementDirection(inputVector);

        float moveDistance = stats.MoveSpeed * Time.deltaTime;
        float playerRadius = 0.7f;
        bool canMove = !Physics.BoxCast(transform.position, Vector3.one * playerRadius, moveDir, Quaternion.identity, moveDistance, countersLayerMask);

        if (!canMove)
        {
            //try to move on X
            Vector3 moveDirX = new Vector3(moveDir.x, 0, 0).normalized;
            canMove = (moveDir.x < -.5f || moveDir.x > .5f) && !Physics.BoxCast(transform.position, Vector3.one * playerRadius, moveDirX, Quaternion.identity, moveDistance, countersLayerMask);
            if (canMove)
            {
                moveDir = moveDirX;

            }
            else
            {
                //can't move on X
                //try to move on Z
                Vector3 moveDirZ = new Vector3(0, 0, moveDir.z).normalized;
                canMove = (moveDir.z < -.5f || moveDir.z > .5f) && !Physics.BoxCast(transform.position, Vector3.one * playerRadius, moveDirZ, Quaternion.identity, moveDistance, countersLayerMask);
                if (canMove)
                {
                    //can move on Z
                    moveDir = moveDirZ;
                }
                else
                {
                    //can't move at all
                }
            }


        }

        if (canMove)
        {
            transform.position += moveDir * moveDistance;

        }


        Context.IsWalking = moveDir != Vector3.zero;

        // Idle frames pass Vector3.zero into Slerp, whose LookRotation normalises a zero
        // vector and logs "Look rotation viewing vector is zero" every frame.
        if (Context.IsWalking)
        {
            float rotateSpeed = 10f;
            transform.forward = Vector3.Slerp(transform.forward, moveDir, rotateSpeed * Time.deltaTime);
        }
    }
    public NetworkObject GetNetworkObject()
    {
        return NetworkObject;
    }

    // ---- Networking ----
    // A click is sent to the server, which runs the counter/bot/plate logic with this player as
    // the actor. Feedback for that player goes through UIManager.ShowAlert and
    // UIManager.ShowOptionMenu.
    // Server: the avatar of a connected client, e.g. to send feedback for one of its RPCs.
    public static PlayerStateMachine FindForClient(ulong clientId)
    {
        NetworkManager networkManager = NetworkManager.Singleton;
        if (networkManager == null || !networkManager.IsServer ||
            !networkManager.ConnectedClients.TryGetValue(clientId, out NetworkClient client) || client.PlayerObject == null)
            return null;
        return client.PlayerObject.GetComponent<PlayerStateMachine>();
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (!all.Contains(this))
            all.Add(this);
        if (IsOwner)
            LocalInstance = this;
        if (MultiplayerManager.Instance != null)
            MultiplayerManager.Instance.OnPlayerDataNetworkListChanged += MultiplayerManager_OnPlayerDataNetworkListChanged;
        SubscribeToOwnerStats();
        SetCharacterMesh();
    }

    public override void OnNetworkDespawn()
    {
        all.Remove(this);
        if (LocalInstance == this)
            LocalInstance = null;
        if (MultiplayerManager.Instance != null)
            MultiplayerManager.Instance.OnPlayerDataNetworkListChanged -= MultiplayerManager_OnPlayerDataNetworkListChanged;
        UnsubscribeFromOwnerStats();
        base.OnNetworkDespawn();
    }

    // The owner's stats (and its account id) can arrive after the avatar spawns; keep trying
    // until they exist, then follow their changes to refresh the look.
    private void SubscribeToOwnerStats()
    {
        if (subscribedOwnerStats != null)
            return;
        GameData gameData = GameManager.Instance != null ? GameManager.Instance.GameData : null;
        if (gameData == null)
            return;
        PlayerStats stats = GetOwnerStats();
        if (stats == null)
        {
            if (subscribedGameData != gameData)
            {
                UnsubscribeFromOwnerStats();
                subscribedGameData = gameData;
                gameData.OnPlayerStatsAdded += GameData_OnPlayerStatsAdded;
            }
            return;
        }
        UnsubscribeFromOwnerStats();
        subscribedOwnerStats = stats;
        stats.OnResourceChange += OnResourceChanged;
    }

    private void UnsubscribeFromOwnerStats()
    {
        if (subscribedOwnerStats != null)
            subscribedOwnerStats.OnResourceChange -= OnResourceChanged;
        subscribedOwnerStats = null;
        if (subscribedGameData != null)
            subscribedGameData.OnPlayerStatsAdded -= GameData_OnPlayerStatsAdded;
        subscribedGameData = null;
    }

    private void GameData_OnPlayerStatsAdded(PlayerStats playerStats)
    {
        RetryOwnerStats();
    }

    private void MultiplayerManager_OnPlayerDataNetworkListChanged(object sender, System.EventArgs e)
    {
        RetryOwnerStats();
    }

    private void RetryOwnerStats()
    {
        if (subscribedOwnerStats != null)
            return;
        SubscribeToOwnerStats();
        if (subscribedOwnerStats != null)
            SetCharacterMesh();
    }

    // Account id of the player who owns this avatar (not necessarily the local player).
    public string GetOwnerPlayerId()
    {
        if (IsOwner && SessionManager.Instance != null)
            return SessionManager.Instance.PlayerId;
        if (MultiplayerManager.Instance == null)
            return null;
        return MultiplayerManager.Instance.GetPlayerDataFromClientId(OwnerClientId).playerId.ToString();
    }

    public PlayerStats GetOwnerStats()
    {
        GameData gameData = GameManager.Instance != null ? GameManager.Instance.GameData : null;
        string playerId = GetOwnerPlayerId();
        if (gameData == null || string.IsNullOrEmpty(playerId))
            return null;
        return gameData.GetPlayerStatsById(playerId);
    }

    private void RequestInteract(IInteractable interactable, bool alternate)
    {
        if (!(interactable is NetworkBehaviour target) || !target.IsSpawned)
            return;
        InteractServerRpc(target, alternate);
    }

    // Client: the HUD start button for a manual-start tool (the Blender). Kept separate from
    // InteractServerRpc so pressing start never doubles as a place/take-back interaction.
    public void RequestStartCooking(NetworkBehaviour target)
    {
        if (!(target is CookingToolCounterController) || !target.IsSpawned)
            return;
        StartCookingServerRpc(target);
    }

    [Rpc(SendTo.Server)]
    private void StartCookingServerRpc(NetworkBehaviourReference targetReference, RpcParams rpcParams = default)
    {
        if (rpcParams.Receive.SenderClientId != OwnerClientId)
            return;
        if (!targetReference.TryGet(out NetworkBehaviour target) || !(target is CookingToolCounterController controller))
            return;
        if (!IsInInteractRange(target))
            return;
        controller.StartCooking();
    }

    // Client (owner): a drag in the close-up put the held item into the tool or took the food out.
    // Same server path and rules as pressing USE at the tool used to be.
    public void RequestInteractFromCloseUp(CookingToolCounterController target)
    {
        if (!IsOwner || target == null || !target.IsSpawned)
            return;
        InteractServerRpc(target, false);
    }

    // Client (owner): report one finished gesture in a cooking tool's close-up.
    public void ReportGesture(CookingToolCounterController target, GestureFamily family, float value)
    {
        if (!IsOwner || target == null || !target.IsSpawned)
            return;
        ReportGestureServerRpc(target, family, value);
    }

    [Rpc(SendTo.Server)]
    private void ReportGestureServerRpc(NetworkBehaviourReference targetReference, GestureFamily family, float value, RpcParams rpcParams = default)
    {
        if (rpcParams.Receive.SenderClientId != OwnerClientId)
            return;
        if (!targetReference.TryGet(out NetworkBehaviour target) || !(target is CookingToolCounterController controller))
            return;
        if (!IsInInteractRange(target))
            return;
        controller.ApplyGesture(this, family, value);
    }

    [Rpc(SendTo.Server)]
    private void InteractServerRpc(NetworkBehaviourReference targetReference, bool alternate, RpcParams rpcParams = default)
    {
        if (rpcParams.Receive.SenderClientId != OwnerClientId)
            return;
        if (!targetReference.TryGet(out NetworkBehaviour target) || !(target is IInteractable interactable))
            return;
        if (!IsInInteractRange(target))
            return;

        if (alternate)
            interactable.InteractAlternateEvent(this);
        else
            interactable.InteractEvent(this);
    }

    // Server: whether this player is close enough to use target.
    public bool IsInInteractRange(NetworkBehaviour target)
    {
        Vector3 position = transform.position;
        Vector3 closest = target.transform.position;
        Collider[] colliders = target.GetComponentsInChildren<Collider>();
        if (colliders.Length > 0)
        {
            Bounds bounds = colliders[0].bounds;
            for (int i = 1; i < colliders.Length; i++)
                bounds.Encapsulate(colliders[i].bounds);
            closest = bounds.ClosestPoint(position);
        }
        Vector2 offset = new Vector2(closest.x - position.x, closest.z - position.z);
        return offset.magnitude <= radius + INTERACT_RANGE_TOLERANCE;
    }
}
