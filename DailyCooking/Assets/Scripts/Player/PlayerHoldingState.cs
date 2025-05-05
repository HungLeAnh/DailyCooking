using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

public class PlayerHoldingState : PlayerBaseState
{
    public PlayerHoldingState(PlayerStateContext context, PlayerStateMachine.EPlayerState stateKey) : base(context, stateKey)
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
            _subStates.Add(PlayerStateMachine.EPlayerState.Holding_Idle, new PlayerHoldingIdleState(Context, PlayerStateMachine.EPlayerState.Holding_Idle));
            _subStates.Add(PlayerStateMachine.EPlayerState.Holding_Walking, new PlayerHoldingWalkState(Context, PlayerStateMachine.EPlayerState.Holding_Walking));
            _isInited = true;
        }
        _currentSubState = _subStates[PlayerStateMachine.EPlayerState.Holding_Idle];

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