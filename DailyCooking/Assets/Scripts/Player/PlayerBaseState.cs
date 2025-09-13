using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem.EnhancedTouch;

public abstract class PlayerBaseState : BaseState<PlayerStateMachine.EPlayerState>
{
    protected PlayerStateContext Context;
    public PlayerBaseState(PlayerStateMachine.EPlayerState stateKey) : base(stateKey)
    {
    }

    public virtual void IntializeStates(PlayerStateContext context)
    {
        Context = context;
    }

    public override void Dispose()
    {
        Context = null;
    }
    public virtual void ChangeAnimationState(string animationName)
    {
        if(Context != null)
        {
            Context.CharacterAnimator.CrossFade(animationName,0.1f,0,0.1f,0.1f);

        }
    }
    public override void UpdateState()
    {
    }
    protected void OnReachDestination()
    {
        Context.IsDisableInput = false;
        Context.NavMeshAgent.isStopped = true;
        Context.IsWalking = false;

        if (Context.SelectedCounterController != null)
        {
            Context.SelectedCounterController.InteractEvent(PlayerStateMachine.Instance);
        }
    }
}
