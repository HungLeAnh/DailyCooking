using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class PlayerStateMachine : PersistentSingleton<PlayerStateMachine>, IKitchenObjectParent
{
    public PlayerStateContext Context { get; set; }

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

    private StateManager<EPlayerState> _stateManager;
    private PlayerStateFactory _stateFactory;

    private void IntializeStates()
    {
        Context = new PlayerStateContext(_characterAnimator,moveSpeed,
            placedObjectView,Instance.transform,countersLayerMask, Instance.kitchenObjectHoldPoint);

        var states = _stateFactory.CreateStates(this);
        _stateManager.SetStates(states, EPlayerState.Idle);
    }
    protected override void Awake()
    {
        base.Awake();
        _stateManager = new StateManager<EPlayerState>();
        _stateFactory = new PlayerStateFactory();
        IntializeStates();
    }
    private void OnDestroy()
    {
        Context = null;
        _stateManager.Dispose();
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
        var state = _stateManager.CurrentState as PlayerBaseState;
        state.MoveTowardsTarget(pathList);

    }
}
