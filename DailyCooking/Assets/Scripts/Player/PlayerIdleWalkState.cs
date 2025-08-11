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
        if (!Context.IsDisableInput && Context.IsWalking)
        {
            UpdatePlayerPosition();
            if (Vector3.Distance(Context.PlayerTransform.position, Context.EndPosition) > GameDefine.MIN_DISTANCE_TO_TARGET)
                return;
            OnReachDestination();
        }
    }
}
