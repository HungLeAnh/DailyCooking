using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.AI;

public class PlayerStateMachine : PersistentSingleton<PlayerStateMachine>, IKitchenObjectParent
{
    public PlayerStateContext Context { get; set; }

    public event EventHandler OnPickedSomething;

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
    private float moveSpeed = 7f;
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
        Context = new PlayerStateContext(characterAnimator, moveSpeed,
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


                if (interactableObject != Context.SelectedInteactableObject)
                {
                    SetInteractableObject(interactableObject);
                    interactableObject.InteractEvent(PlayerStateMachine.Instance);
                }
                else if (interactableObject == Context.SelectedInteactableObject)
                {
                    if (Context.SelectedInteactableObject != null)
                    {
                        IHasProgress progress = Context.SelectedInteactableObject as IHasProgress;
                        if (progress == null)
                        {
                            SetInteractableObject(interactableObject);
                            Context.SelectedInteactableObject.InteractEvent(PlayerStateMachine.Instance);
                        }
                        else
                        {

                            if (progress.IsDone())
                            {
                                Context.SelectedInteactableObject.InteractEvent(PlayerStateMachine.Instance);

                            }
                            else
                            {

                                Context.SelectedInteactableObject.InteractAlternateEvent(PlayerStateMachine.Instance);
                            }
                        }
                    }
                }
            }
            else
            {
                SetInteractableObject(null);
            }
        }
        else
        {
            SetInteractableObject(null);
        }
    }
    private void HandleMovement()
    {
        Vector2 inputVector = Context.PlayerGameInput.GetMovementVectorNormalized();


        Vector3 moveDir = new Vector3(inputVector.x, 0, inputVector.y);

        float moveDistance = moveSpeed * Time.deltaTime;
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
    private void SetInteractableObject(IInteractable interactable)
    {

        //Context.SelectedInteactableObject?.OnDeselected();
        //if (interactable != null)
        //    interactable.OnSelected();
        Context.SelectedInteactableObject = interactable;
    }
}
