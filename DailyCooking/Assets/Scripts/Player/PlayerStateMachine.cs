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

    private void SetCharacterMesh()
    {
        foreach (var part in customizationParts)
        {
            part.Initialise(ConfigManager.Instance.CustomizationData);
        }
        var playerData = GameManager.Instance.GameData.GetPlayerStatsById(SessionManager.Instance.PlayerId);
        foreach (var item in playerData.CharacterCustomizationIds)
        {
            var part = customizationParts.FirstOrDefault(x => x.Type == item.Key);
            if (part != null)
            {
                if (item.Value >= 0)
                    part.SetMesh(item.Value);
                else
                    part.Clear();
            }
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
        GameManager.Instance.GameData.GetPlayerStatsById(SessionManager.Instance.PlayerId).OnResourceChange += OnResourceChanged;
    }

    private void OnResourceChanged()
    {
        SetCharacterMesh();
    }

    private void OnDestroy()
    {
        if(GameInput.Instance != null)
            GameInput.Instance.OnMouseClickPerformed -= PlayerStateMachine_OnMouseClickPerformed;

        Context = null;
        _stateManager.Dispose();
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

        if (Context.IsDisableInput)
            return;

        float maxDistance = 999f;
        Ray ray = Camera.main.ScreenPointToRay(e);
        if (Physics.Raycast(ray, out RaycastHit raycastHit, maxDistance))
        {
            if (raycastHit.transform.TryGetComponent(out IInteractable interactableObject))
            {
                if (!Context.Highlightable.Contains(raycastHit.transform.GetComponent<IHighlightable>()))
                    return;


                if (interactableObject != Context.SelectedInteractableObject)
                {
                    SetInteractableObject(interactableObject, raycastHit.transform);
                    Context.SelectedInteractableObject.InteractEvent(this);
                }
                else if (interactableObject == Context.SelectedInteractableObject)
                {
                    if (Context.SelectedInteractableObject != null)
                    {
                        IHasProgress progress = Context.SelectedInteractableObject as IHasProgress;
                        if (progress == null)
                        {
                            SetInteractableObject(interactableObject, raycastHit.transform);
                            Context.SelectedInteractableObject.InteractEvent(this);
                        }
                        else
                        {

                            if (progress.IsDone() || progress.GetProgress() == -1)
                            {
                                Context.SelectedInteractableObject.InteractEvent(this);
                            }
                            else
                            {
                                Context.SelectedInteractableObject.InteractAlternateEvent(this);
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

        Collider[] colliderHitArray = Physics.OverlapCapsule(p1, p2, radius, countersLayerMask);
        foreach (IHighlightable highlightable in Context.Highlightable)
        {
            if(highlightable as UnityEngine.Object != null)
                highlightable.OnDeselected();
        }
        if (colliderHitArray.Length > 0)
        {
            List<IHighlightable> newHighlightables = new List<IHighlightable>(Context.Highlightable);
            foreach (Collider hit in colliderHitArray)
            {
                if (hit.transform.TryGetComponent(out IHighlightable highlightable))
                {
                    highlightable.OnSelected();
                    newHighlightables.Add(highlightable);
                }
            }
            Context.Highlightable.Clear();
            Context.Highlightable = newHighlightables;

        }
        else
        {
            Context.Highlightable.Clear();
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
        if (GameManager.Instance.GameData == null)
            return;

        Vector2 inputVector = Context.PlayerGameInput.GetMovementVectorNormalized();

        Vector3 moveDir = new Vector3(inputVector.x, 0, inputVector.y);

        float moveDistance = GameManager.Instance.GameData.GetPlayerStatsById(SessionManager.Instance.PlayerId).MoveSpeed * Time.deltaTime;
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
}
