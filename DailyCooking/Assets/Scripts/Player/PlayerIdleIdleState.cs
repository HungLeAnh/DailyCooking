using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerIdleIdleState : PlayerIdleState
{
    public PlayerIdleIdleState(PlayerStateMachine.EPlayerState stateKey) : base(stateKey)
    {
    }

    public override void EnterState()
    {
        ChangeAnimationState("Idle");
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
    }
}
