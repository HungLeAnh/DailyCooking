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
        GameInput.Instance.OnInteract1Performed += PlayerStateMachine_OnInteract1Performed;
        GameInput.Instance.OnInteract2Performed += PlayerStateMachine_OnInteract2Performed;
    }



    private void OnDestroy()
    {
        GameInput.Instance.OnInteract1Performed -= PlayerStateMachine_OnInteract1Performed;
        GameInput.Instance.OnInteract2Performed -= PlayerStateMachine_OnInteract2Performed;
        Context = null;
        _stateManager.Dispose();
    }
    private void Update()
    {
        _stateManager.Update();

        HandleMovement();
        HandleInteractions();
    }

    public Transform GetKitchenObjectFollowTransform()
    {
        return kitchenObjectHoldPoint;
    }

    public void SetKitchenObject(KitchenObject kitchenObject)
    {
        Context.KitchenObject = kitchenObject;

        if (kitchenObject != null)
        {
            OnPickedSomething?.Invoke(this, EventArgs.Empty);
        }
    }

    public KitchenObject GetKitchenObject()
    {
        return Context.KitchenObject;
    }

    public void ClearKitchenObject()
    {
        Context.KitchenObject = null;
    }

    public bool HasKitchenObject()
    {
        return Context.KitchenObject != null;
    }
    public void DisableInput(bool isDisable)
    {
        Context.IsDisableInput = isDisable;
    }

    private void PlayerStateMachine_OnInteract2Performed(object sender, EventArgs e)
    {
        Context.SelectedInteactableObject?.InteractAlternateEvent(PlayerStateMachine.Instance);
    }
    private void PlayerStateMachine_OnInteract1Performed(object sender, EventArgs e)
    {
        Context.SelectedInteactableObject?.InteractEvent(PlayerStateMachine.Instance);
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
        if (Physics.Raycast(transform.position, Context.LastInteractDir,
            out RaycastHit raycastHit, interactDistance))
        {
            if (raycastHit.transform.TryGetComponent(out IInteractable interactableObject))
            {
                if (interactableObject != Context.SelectedInteactableObject)
                {
                    SetInteractableObject(interactableObject);
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
        Context.SelectedInteactableObject = interactable;
    }
    private void SetSelectedCounter(BaseCounterController selectedCounter)
    {
        Context.SelectedCounterController = selectedCounter;
        CounterModules.Instance.FireOnSelectedCounterChanged(new CounterModules.OnSelectedCounterChangedEventArgs
        {
            selectedCounterController = selectedCounter != null ? selectedCounter : null

        });

    }
}
