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
        // Movement logic moved to specific walk states
    }
    
    protected void UpdatePlayerPosition()
    {
        if (Context.IsWalking && Context.PathList.Count > 0)
        {
            // Calculate the step size
            float step = Context.MoveSpeed * Time.deltaTime;
            var nextTarget = GridBuildingSystem.Instance.GridPositionToWorldPosition(Context.PathList[Context.WayPointIndex]);

            // Move the character towards the target
            Context.PlayerTransform.position = Vector3.MoveTowards(Context.PlayerTransform.position, nextTarget, step);

            // Optionally, rotate the character to face the target
            Vector3 targetDirection = nextTarget - Context.PlayerTransform.position;
            Vector3 newDirection = Vector3.RotateTowards(Context.PlayerTransform.forward, targetDirection, step, 0.0f);
            Context.PlayerTransform.rotation = Quaternion.LookRotation(newDirection);

            if (Vector3.Distance(Context.PlayerTransform.position, nextTarget) < GameDefine.MIN_DISTANCE_TO_TARGET)
            {
                Context.WayPointIndex++;
                if (Context.WayPointIndex >= Context.PathList.Count)
                {
                    Context.IsWalking = false;
                }
            }
        }
    }
    protected void StopMove()
    {
        Context.IsWalking = false;
    }

    protected void MoveTowardsTarget(int2 target, float speed = 0)
    {

        Context.IsWalking = true;
        Context.WayPointIndex = 0;
        Context.PathList.Clear();
        Context.PathList = new List<int2>() { target };
        Context.EndPosition = GridBuildingSystem.Instance.GridPositionToWorldPosition(target);

        if (speed != 0)
            Context.MoveSpeed = speed;
    }

    public void MoveTowardsTarget(List<int2> target, float speed = 0)
    {
        if (target.Count == 0)
            return;
        Context.IsWalking = true;
        Context.WayPointIndex = 0;
        Context.PathList.Clear();
        Context.PathList = target;
        
        Context.EndPosition = GridBuildingSystem.Instance.GridPositionToWorldPosition(target[target.Count - 1]);

        if (speed != 0)
            Context.MoveSpeed = speed;
    }
    
    protected void OnReachDestination()
    {
        Context.IsReachedDestination = true;
        if (Context.SelectedCounterController != null)
        {
            Context.SelectedCounterController.InteractEvent(PlayerStateMachine.Instance);
        }
    }
}
