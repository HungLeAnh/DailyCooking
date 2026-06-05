using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

public class PlayerWalkState : PlayerBaseState
{
    public PlayerWalkState(PlayerStateMachine.EPlayerState stateKey) : base(stateKey)
    {
    }

    public override void EnterState()
    {
        UpdateAnimation();
    }

    public override void ExitState()
    {
    }

    public override PlayerStateMachine.EPlayerState GetNextState()
    {
        if (!Context.IsWalking)
            return PlayerStateMachine.EPlayerState.Idle;
        
        return PlayerStateMachine.EPlayerState.Walking;
    }

    public override void UpdateState()
    {
        UpdateAnimation();
    }

    private void UpdateAnimation()
    {
        ChangeAnimationState("Walking");
        if (Context.PlayerIKHandler != null)
        {
            float targetWeight = Context.KitchenObject != null ? 1f : 0f;
            Context.PlayerIKHandler.SetIKWeight(targetWeight);
        }
    }
}
