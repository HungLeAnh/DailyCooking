using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.AI;

public class PlayerStateMachine : PersistentSingleton<PlayerStateMachine>, IKitchenObjectParent
{
    public PlayerStateContext Context { get; set; }

    public event EventHandler OnPickedSomething;
    public event EventHandler<Transform> OnObjectHighlighted;
    public event EventHandler<Transform> OnSelectInteractable;
    public enum EPlayerState
    {
        // Top-level states
        Idle, 
        Holding,

        // Sub-states for Idle
        Idle_Idle, 
        Idle_Walking,

        // Sub-states for Holding
        Holding_Idle,
        Holding_Walking,
    }


    [SerializeField]
    private LayerMask countersLayerMask;
    [SerializeField] 
    private Transform kitchenObjectHoldPoint;
    [SerializeField] 
    private Animator characterAnimator;


    private StateManager<EPlayerState> _stateManager;
    private PlayerStateFactory _stateFactory;


    private void IntializeStates()
    {
        Context = new PlayerStateContext(characterAnimator,
            this.transform, countersLayerMask, this.kitchenObjectHoldPoint);

        var states = _stateFactory.CreateStates(this);
        _stateManager.SetStates(states, EPlayerState.Idle);
        _stateManager.Start();
    }

    protected override void Awake()
    {
        base.Awake();
        _stateManager = new StateManager<EPlayerState>();
        _stateFactory = new PlayerStateFactory();
        IntializeStates();

    }
    private void Start()
    {
        GameInput.Instance.OnMouseClickPerformed += PlayerStateMachine_OnMouseClickPerformed;
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
                if (Vector3.Distance(raycastHit.transform.position, Context.PlayerTransform.position) >
                    GameDefine.INTERACT_DISTANCE_MAX)
                    return;


                if (interactableObject != Context.SelectedInteractableObject)
                {
                    SetInteractableObject(interactableObject, raycastHit.transform);
                    Context.SelectedInteractableObject.InteractEvent(PlayerStateMachine.Instance);
                }
                else if (interactableObject == Context.SelectedInteractableObject)
                {
                    if (Context.SelectedInteractableObject != null)
                    {
                        IHasProgress progress = Context.SelectedInteractableObject as IHasProgress;
                        if (progress == null)
                        {
                            SetInteractableObject(interactableObject, raycastHit.transform);
                            Context.SelectedInteractableObject.InteractEvent(PlayerStateMachine.Instance);
                        }
                        else
                        {

                            if (progress.IsDone() || progress.GetProgress() == -1)
                            {
                                Context.SelectedInteractableObject.InteractEvent(PlayerStateMachine.Instance);
                            }
                            else
                            {
                                Context.SelectedInteractableObject.InteractAlternateEvent(PlayerStateMachine.Instance);
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

        float interactDistance = 2f;
        if (Physics.Raycast(transform.position, Context.LastInteractDir, out RaycastHit raycastHit, interactDistance, countersLayerMask))
        {
            if (raycastHit.transform.TryGetComponent(out IHighlightable highlightable))
            {
                if (Context.Highlightable != null)
                    Context.Highlightable.OnDeselected();
                Context.Highlightable = highlightable;
                Context.Highlightable.OnSelected();
                OnObjectHighlighted?.Invoke(this, raycastHit.transform);
                
            }
            else
            {
                if (Context.Highlightable != null)
                    Context.Highlightable.OnDeselected();
            }
        }
        else
        {
            if (Context.Highlightable != null)
                Context.Highlightable.OnDeselected();
        }

    }
    private void HandleMovement()
    {
        Vector2 inputVector = Context.PlayerGameInput.GetMovementVectorNormalized();


        Vector3 moveDir = new Vector3(inputVector.x, 0, inputVector.y);

        float moveDistance = GameManager.Instance.GameData.PlayerStats.statsData.MoveSpeed * Time.deltaTime;
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
}
