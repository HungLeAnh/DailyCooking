using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
public class PlayerIdleState : PlayerBaseState
{
    public PlayerIdleState(PlayerStateContext context, PlayerStateMachine.EPlayerState stateKey) : base(context, stateKey)
    {
        Context = context;

    }

    public override void EnterState()
    {
        if (_currentSubState != null)
            _currentSubState.EnterState();
    }

    public override void ExitState()
    {
    }

    public override PlayerStateMachine.EPlayerState GetNextState()
    {
        if (Context.KitchenObject != null)
            return PlayerStateMachine.EPlayerState.Holding;
        else
            return PlayerStateMachine.EPlayerState.Idle;
    }

    public override void IntializeStates()
    {
        if (!_isInited)
        {
            _subStates.Add(PlayerStateMachine.EPlayerState.Idle_Walking, new PlayerIdleWalkState(Context, PlayerStateMachine.EPlayerState.Idle_Walking));
            _subStates.Add(PlayerStateMachine.EPlayerState.Idle_Idle, new PlayerIdleIdleState(Context, PlayerStateMachine.EPlayerState.Idle_Idle));
            _isInited = true;
        }
        _currentSubState = _subStates[PlayerStateMachine.EPlayerState.Idle_Idle];
    }

    public override void OnTriggerEnter(Collider other)
    {
    }

    public override void OnTriggerExit(Collider other)
    {
    }

    public override void OnTriggerStay(Collider other)
    {
    }

    public override void UpdateState()
    {
        base.UpdateState();
        base.UpdateSubState();
    }
}
