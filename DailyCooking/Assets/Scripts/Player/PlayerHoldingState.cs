using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

public class PlayerHoldingState : PlayerBaseState
{
    public PlayerHoldingState(PlayerStateMachine.EPlayerState stateKey) : base(stateKey)
    {

    }

    public override void EnterState()
    {
        _subStateMachine.Start();
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

    public override void IntializeStates(PlayerStateContext context)
    {
        base.IntializeStates(context);
        if (!_isInited)
        {
            var subStates = new Dictionary<PlayerStateMachine.EPlayerState, IState<PlayerStateMachine.EPlayerState>>
            {
                { PlayerStateMachine.EPlayerState.Holding_Idle, new PlayerHoldingIdleState(PlayerStateMachine.EPlayerState.Holding_Idle) },
                { PlayerStateMachine.EPlayerState.Holding_Walking, new PlayerHoldingWalkState(PlayerStateMachine.EPlayerState.Holding_Walking) }
            };
            foreach (var key in subStates.Keys)
            {
                (subStates[key] as PlayerBaseState).IntializeStates(Context);
            }
            _subStateMachine.SetStates(subStates, PlayerStateMachine.EPlayerState.Holding_Idle);
            _isInited = true;
        }
    }
    public override void UpdateState()
    {
        _subStateMachine.Update();
    }
}