using Cinemachine;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerStateMachine : MonoStateManager<PlayerStateMachine.EPlayerState>, IKitchenObjectParent
{
    private static PlayerStateMachine _instance;

    public static PlayerStateMachine Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<PlayerStateMachine>();

                if (_instance == null)
                {
                    GameObject gameObject = new GameObject(nameof(PlayerStateMachine));
                    _instance = gameObject.AddComponent<PlayerStateMachine>();
                }
            }
            return _instance;

        }
    }

    public event EventHandler OnPickedSomething;

    public event EventHandler<OnSelectedCounterChangedEventArgs> OnSelectedCounterChanged;
    public class OnSelectedCounterChangedEventArgs : EventArgs
    {
        public BaseCounter selectedCounter;
    }
    public enum EPlayerState
    {
        Idle, 
        Idle_Idle, 
        Idle_Walking,
        Holding,
        Holding_Idle,
        Holding_Walking,
    }

    [SerializeField]
    private float moveSpeed = 7f;
    [SerializeField]
    private GameInput _gameInput;
    [SerializeField]
    private LayerMask countersLayerMask;
    [SerializeField] 
    private Transform kitchenObjectHoldPoint;
    [SerializeField] 
    private Animator _characterAnimator;

    private PlayerStateContext _context;

    public void IntializeStates()
    {
        _context = new PlayerStateContext(_characterAnimator,_gameInput,moveSpeed,
                                            transform,countersLayerMask, kitchenObjectHoldPoint);

        _states.Add(EPlayerState.Idle, new PlayerIdleState(_context, EPlayerState.Idle));
        _states.Add(EPlayerState.Holding,new PlayerHoldingState(_context,EPlayerState.Holding));
        foreach (var key in _states.Keys)
        {
            _states[key].IntializeStates();
        }
        _currentState = _states[EPlayerState.Idle];
    }
    private void Awake()
    {
        if (_instance == null)
            _instance = this as PlayerStateMachine;
        else
        {
            if (_instance != this)
            {
                Destroy(gameObject);
            }
        }
        IntializeStates();
    }

    private void Start()
    {
        _gameInput.OnInteractAction += GameInput_OnInteractAction;
        _gameInput.OnInteractAction2 += GameInput_OnInteractAlternateAction;
    }
    private void GameInput_OnInteractAlternateAction(object sender, EventArgs e)
    {
        if (_context.SelectedCounter != null)
        {
            _context.SelectedCounter.InteractAlternate(this);
        }
    }
    public void FireOnSelectedCounterChanged(OnSelectedCounterChangedEventArgs args)
    {
        OnSelectedCounterChanged?.Invoke(this,args);
    }
    private void GameInput_OnInteractAction(object sender, EventArgs e)
    {
        if (_context.SelectedCounter != null)
        {
            _context.SelectedCounter.Interact(this);
        }
    }
    
    public Transform GetKitchenObjectFollowTransform()
    {
        return kitchenObjectHoldPoint;
    }

    public void SetKitchenObject(KitchenObject kitchenObject)
    {
        _context.KitchenObject = kitchenObject;

        if (kitchenObject != null)
        {
            OnPickedSomething?.Invoke(this, EventArgs.Empty);
        }
    }

    public KitchenObject GetKitchenObject()
    {
        return _context.KitchenObject;
    }

    public void ClearKitchenObject()
    {
        _context.KitchenObject = null;
    }

    public bool HasKitchenObject()
    {
        return _context.KitchenObject != null;
    }
}

public abstract class PlayerBaseState : BaseState<PlayerStateMachine.EPlayerState>
{
    protected PlayerStateContext Context;
    public PlayerBaseState(PlayerStateContext context,PlayerStateMachine.EPlayerState stateKey) : base(stateKey)
    {
        Context = context;
    }    

    public virtual void ChangeAnimationState(string animationName)
    {
        Context.CharacterAnimator.Play(animationName);
    }
    public override void UpdateState()
    {
        Update();
        HandleMovement();
        HandleInteractions();
    }
    private void HandleMovement()
    {
        Vector2 inputVector = Context.PlayerGameInput.GetMovementVectorNormalized();

        Debug.Log("input Vector: " + inputVector);
        Vector3 moveDir = new Vector3(inputVector.x, 0, inputVector.y);

        float moveDistance = Context.MoveSpeed * Time.deltaTime;
        float playerRadius = 0.7f;
        float playerHeight = 2f;
        bool canMove = !Physics.CapsuleCast(Context.PlayerTransform.position, Context.PlayerTransform.position + Vector3.up * playerHeight, playerRadius, moveDir, moveDistance);

        if (!canMove)
        {
            //try to move on X
            Vector3 moveDirX = new Vector3(moveDir.x, 0, 0).normalized;
            canMove = (moveDir.x < -.5f || moveDir.x > .5f) && !Physics.CapsuleCast(Context.PlayerTransform.position, Context.PlayerTransform.position + Vector3.up * playerHeight, playerRadius, moveDirX, moveDistance);
            if (canMove)
            {
                moveDir = moveDirX;

            }
            else
            {
                //can't move on X
                //try to move on Z
                Vector3 moveDirZ = new Vector3(0, 0, moveDir.z).normalized;
                canMove = (moveDir.z < -.5f || moveDir.z > .5f) && !Physics.CapsuleCast(Context.PlayerTransform.position, Context.PlayerTransform.position + Vector3.up * playerHeight, playerRadius, moveDirZ, moveDistance);
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
            Context.PlayerTransform.position += moveDir * moveDistance;

        }


        Context.IsWalking = moveDir != Vector3.zero;
        
        float rotateSpeed = 10f;
        
        if(Context.IsWalking)
            Context.PlayerTransform.forward = Vector3.Slerp(Context.PlayerTransform.forward, moveDir, rotateSpeed * Time.deltaTime);
    }
    private void HandleInteractions()
    {
        Vector2 inputVector = Context.PlayerGameInput.GetMovementVectorNormalized();


        Vector3 moveDir = new Vector3(inputVector.x, 0, inputVector.y);

        if (moveDir != Vector3.zero)
        {
            Context.LastInteractDir = moveDir;
        }

        float interactDistance = 2f;
        if (Physics.Raycast(Context.PlayerTransform.position, Context.LastInteractDir, out RaycastHit raycastHit, interactDistance, Context.CounterLayerMask))
        {
            if (raycastHit.transform.TryGetComponent(out BaseCounter baseCounter))
            {
                if (baseCounter != Context.SelectedCounter)
                {
                    SetSelectedCounter(baseCounter);
                }
            }
            else
            {
                SetSelectedCounter(null);
            }
        }
        else
        {
            SetSelectedCounter(null);
        }

    }
    private void SetSelectedCounter(BaseCounter selectedCounter)
    {
        Context.SelectedCounter = selectedCounter;
        PlayerStateMachine.Instance.FireOnSelectedCounterChanged(new PlayerStateMachine.OnSelectedCounterChangedEventArgs
        {
            selectedCounter = selectedCounter

        });
    }

}
