using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
public class PlayerIdleWalkState : PlayerBaseState
{
    public PlayerIdleWalkState(PlayerStateMachine.EPlayerState stateKey) : base(stateKey)
    {
    }

    public override void EnterState()
    {
        ChangeAnimationState("IdleWalk");
    }

    public override void ExitState()
    {
    }

    public override PlayerStateMachine.EPlayerState GetNextState()
    {
        if (!Context.IsWalking)
            return PlayerStateMachine.EPlayerState.Idle_Idle;
        else
            return PlayerStateMachine.EPlayerState.Idle_Walking;
    }
    public override void UpdateState()
    {
        base.UpdateState();
        _subStateMachine.Update();
    }
}
