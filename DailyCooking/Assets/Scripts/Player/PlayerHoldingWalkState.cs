using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
public class PlayerHoldingWalkState : PlayerBaseState
{
    public PlayerHoldingWalkState(PlayerStateMachine.EPlayerState stateKey) : base(stateKey)
    {
    }

    public override void EnterState()
    {
        ChangeAnimationState("HoldingWalk");
    }

    public override void ExitState()
    {
    }

    public override PlayerStateMachine.EPlayerState GetNextState()
    {
        if (!Context.IsWalking)
            return PlayerStateMachine.EPlayerState.Holding_Idle;
        else
            return PlayerStateMachine.EPlayerState.Holding_Walking;
    }

    public override void UpdateState()
    {
        base.UpdateState();
        _subStateMachine.Update();
    }
}