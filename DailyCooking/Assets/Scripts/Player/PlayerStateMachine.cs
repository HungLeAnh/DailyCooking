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

    public event EventHandler OnPickedSomething;
    public event EventHandler<Transform> OnObjectHighlighted;
    public event EventHandler<Transform> OnSelectInteractable;
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
        GameInput.Instance.OnMouseClickPerformed += PlayerStateMachine_OnMouseClickPerformed;
    }

    private void OnResourceChanged()
    {
        SetCharacterMesh();
    }

    public override void OnDestroy()
    {
        if(GameInput.Instance != null)
            GameInput.Instance.OnMouseClickPerformed -= PlayerStateMachine_OnMouseClickPerformed;

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
    private void PlayerStateMachine_OnMouseClickPerformed(object sender, Vector2 e)
    {
        // Every avatar hears the click; only the local player's own avatar acts on it.
        if (!IsOwner || Context.IsDisableInput)
            return;

        float maxDistance = 999f;
        Ray ray = Camera.main.ScreenPointToRay(e);
        if (Physics.Raycast(ray, out RaycastHit raycastHit, maxDistance))
        {
            // Interactables and tool meshes can live on child transforms: walk up the hierarchy.
            IInteractable interactableObject = raycastHit.transform.GetComponentInParent<IInteractable>();
            if (interactableObject == null)
            {
                // Clicked a stacked tool (not IInteractable itself): forward to its counter's controller.
                CookingTool tool = raycastHit.transform.GetComponentInParent<CookingTool>();
                if (tool != null)
                    interactableObject = CookingToolCounterController.FindControllerForTool(tool);
            }
            if (interactableObject != null)
            {
                IHighlightable gate = raycastHit.transform.GetComponentInParent<IHighlightable>();
                if (gate == null && interactableObject is Component interactableComponent)
                    gate = interactableComponent.GetComponent<IHighlightable>();
                if (gate != null && !Context.Highlightable.Contains(gate))
                    return;


                if (interactableObject != Context.SelectedInteractableObject)
                {
                    SetInteractableObject(interactableObject, raycastHit.transform);
                    RequestInteract(Context.SelectedInteractableObject, false);
                }
                else if (interactableObject == Context.SelectedInteractableObject)
                {
                    if (Context.SelectedInteractableObject != null)
                    {
                        IHasProgress progress = Context.SelectedInteractableObject as IHasProgress;
                        if (progress == null)
                        {
                            SetInteractableObject(interactableObject, raycastHit.transform);
                            RequestInteract(Context.SelectedInteractableObject, false);
                        }
                        else
                        {

                            if (progress.IsDone() || progress.GetProgress() == -1)
                            {
                                RequestInteract(Context.SelectedInteractableObject, false);
                            }
                            else
                            {
                                RequestInteract(Context.SelectedInteractableObject, true);
                            }
                        }
                    }
                }
            }
            else
            {
                SetInteractableObject(null, null);
            }
        }
        else
        {
            SetInteractableObject(null,null);
        }
    }
    private void HandleInteractions()
    {
        Vector2 inputVector = GameInput.Instance.GetMovementVectorNormalized();


        Vector3 moveDir = new Vector3(inputVector.x, 0, inputVector.y);

        if (moveDir != Vector3.zero)
        {
            Context.LastInteractDir = moveDir;
        }

        Vector3 p1 = transform.position + Vector3.up * radius;
        Vector3 p2 = transform.position + Vector3.up * (height - radius);

        // Runs every frame: reuse buffers and only toggle what entered or left the range.
        int hitCount = Physics.OverlapCapsuleNonAlloc(p1, p2, radius, highlightHitBuffer, countersLayerMask);
        nearbyHighlightables.Clear();
        for (int i = 0; i < hitCount; i++)
        {
            if (highlightHitBuffer[i].transform.TryGetComponent(out IHighlightable highlightable) &&
                !nearbyHighlightables.Contains(highlightable))
                nearbyHighlightables.Add(highlightable);
        }

        List<IHighlightable> current = Context.Highlightable;
        foreach (IHighlightable highlightable in current)
        {
            if (highlightable as UnityEngine.Object != null && !nearbyHighlightables.Contains(highlightable))
                highlightable.OnDeselected();
        }
        foreach (IHighlightable highlightable in nearbyHighlightables)
        {
            if (!current.Contains(highlightable))
                highlightable.OnSelected();
        }
        current.Clear();
        current.AddRange(nearbyHighlightables);


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

        Vector2 inputVector = Context.PlayerGameInput.GetMovementVectorNormalized();

        Vector3 moveDir = new Vector3(inputVector.x, 0, inputVector.y);

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

        float rotateSpeed = 10f;
        transform.forward = Vector3.Slerp(transform.forward, moveDir, rotateSpeed * Time.deltaTime);
    }
    private void SetInteractableObject(IInteractable interactable,Transform transform)
    {
        Context.SelectedInteractableObject = interactable;
        if (interactable != null)
            OnSelectInteractable?.Invoke(this, transform);
    }

    public NetworkObject GetNetworkObject()
    {
        return NetworkObject;
    }

    // ---- Networking ----
    // A click is sent to the server, which runs the counter/bot/plate logic with this player as
    // the actor. Feedback for that player goes through UIManager.ShowAlert and
    // InteractionUI.ShowOptionMenu.
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
        if (IsOwner)
            LocalInstance = this;
        if (MultiplayerManager.Instance != null)
            MultiplayerManager.Instance.OnPlayerDataNetworkListChanged += MultiplayerManager_OnPlayerDataNetworkListChanged;
        SubscribeToOwnerStats();
        SetCharacterMesh();
    }

    public override void OnNetworkDespawn()
    {
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
