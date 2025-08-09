using System;
using System.Collections.Generic;
using Unity.Mathematics;
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
                _instance = FindFirstObjectByType<PlayerStateMachine>();
            }
            return _instance;

        }
    }

    public PlayerStateContext Context { get => _context; set => _context = value; }

    public event EventHandler OnPickedSomething;

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
    private LayerMask countersLayerMask;
    [SerializeField] 
    private Transform kitchenObjectHoldPoint;
    [SerializeField] 
    private Animator _characterAnimator;
    [SerializeField]
    private PlacedObjectView placedObjectView;

    private PlayerStateContext _context;



    public void IntializeStates()
    {
        Context = new PlayerStateContext(_characterAnimator,moveSpeed,
            placedObjectView,Instance.transform,countersLayerMask, Instance.kitchenObjectHoldPoint);

        _states.Add(EPlayerState.Idle, new PlayerIdleState(EPlayerState.Idle));
        _states.Add(EPlayerState.Holding,new PlayerHoldingState(EPlayerState.Holding));
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
    private void OnDestroy()
    {
        Context = null;
        foreach (var state in _states)
        {
            state.Value.Dispose();
        }
        _states.Clear();
        _currentState = null;
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
    public void SetPlayerPath(List<int2> pathList)
    {
        pathList.Reverse();
        var state = _currentState as PlayerBaseState;
        state.MoveTowardsTarget(pathList);

    }
}
