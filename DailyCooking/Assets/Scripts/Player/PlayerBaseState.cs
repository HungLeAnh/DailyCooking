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
            if (Context.CurrentAnimation == animationName) return;

            Context.CharacterAnimator.CrossFade(animationName,0.1f,0,0.1f,0.1f);
            Context.CurrentAnimation = animationName;
        }
    }
    public override void UpdateState()
    {
    }
}
