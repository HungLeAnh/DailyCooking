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
        public BaseCounterView selectedCounterView;
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
        //_gameInput.OnInteractAction += GameInput_OnInteractAction;
        //_gameInput.OnInteractAction2 += GameInput_OnInteractAlternateAction;
    }
    private void GameInput_OnInteractAlternateAction(object sender, EventArgs e)
    {
        if (_context.SelectedCounter != null)
        {
            _context.SelectedCounter.FireInteractAlternateEvent(this);
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
            _context.SelectedCounter.FireInteractEvent(this);
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
    public void DisableInput(bool isDisable)
    {
        _context.IsDisableInput = isDisable;
    }
}
