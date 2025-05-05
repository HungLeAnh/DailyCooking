using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem.EnhancedTouch;

public abstract class PlayerBaseState : BaseState<PlayerStateMachine.EPlayerState>
{
    protected PlayerStateContext Context;
    public PlayerBaseState(PlayerStateContext context,PlayerStateMachine.EPlayerState stateKey) : base(stateKey)
    {
        Context = context;
        //Context.PlayerGameInput.OnFingerDown += PlayerGameInput_OnFingerDown;
        Context.PlayerGameInput.OnTouchPerformed += PlayerGameInput_OnFingerDown;

    }

    public virtual void ChangeAnimationState(string animationName)
    {
        Context.CharacterAnimator.CrossFade(animationName,0.1f,0,0.1f,0.1f);
    }
    public void UpdatePlayerPosition()
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

            if (Vector3.Distance(Context.PlayerTransform.position, nextTarget) < 0.05f)
            {
                //Debug.Log($"Next waypoint {Context.WayPointIndex}:{Context.PathList[Context.WayPointIndex]}");
                Context.WayPointIndex++;
                if (Context.WayPointIndex >= Context.PathList.Count)
                {
                    Context.IsWalking = false;
                    //Debug.Log("Arrived");
                }
            }
        }
    }
    public void StopMove()
    {
        Context.IsWalking = false;
    }

    public void MoveTowardsTarget(int2 target, float speed = 0)
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
        Context.IsWalking = true;
        Context.WayPointIndex = 0;
        Context.PathList.Clear();
        Context.PathList = target;
        
        Context.EndPosition = GridBuildingSystem.Instance.GridPositionToWorldPosition(target[target.Count - 1]);

        if (speed != 0)
            Context.MoveSpeed = speed;
    }
    public override void UpdateState()
    {
        if (!Context.IsDisableInput && Context.IsWalking)
        {
            UpdatePlayerPosition();
            //Debug.Log("Distance: "+ Vector2.Distance(Context.PlayerTransform.position, Context.EndPosition));
            if (Vector3.Distance(Context.PlayerTransform.position, Context.EndPosition) > 0.05f)
                return;
            Debug.Log("OnReachDestination");
            OnReachDestination();

        }
    }

    private void OnReachDestination()
    {
        if (Context.SelectedCounter != null)
        {
            Context.SelectedCounter.FireInteractEvent(PlayerStateMachine.Instance);
        }
    }

    private void PlayerGameInput_OnFingerDown(object sender, Finger finger)
    {
        if(finger.currentTouch.delta.sqrMagnitude >= 0.1f)
            return;
        
        
        float interactDistance = 999f;
        if (!Camera.main.pixelRect.Contains(finger.screenPosition))
            return;
        Ray ray = Camera.main.ScreenPointToRay(finger.screenPosition);
        if (Physics.Raycast(ray, out RaycastHit raycastHit, interactDistance, Context.CounterLayerMask))
        {
            //Debug.Log("Touch Position: " + pos);
            if (raycastHit.transform.TryGetComponent(out BaseCounterView baseCounter))
            {
                if (baseCounter != Context.SelectedCounter)
                {
                    //Debug.LogError("Selected Counter: " + baseCounter.name);
                    SetSelectedCounter(baseCounter);
                    if (baseCounter.gameObject.TryGetComponent(out PlacedObjectView placedObjectView))
                    {
                        int2 counterOrigin = new int2(placedObjectView.GetModel().Origin.x, placedObjectView.GetModel().Origin.y);
                        int2 playerPos = GridBuildingSystem.Instance.WorldPositionToGridPos(Context.PlayerTransform.position.x, Context.PlayerTransform.position.z);
                        //Debug.Log("Counter Origin: " + counterOrigin);
                        GridBuildingSystem.Instance.FindPath(playerPos, counterOrigin);
                    }
                }
            }
            else
            {
                SetSelectedCounter(null);
            }
        }
        else
        {
            SetSelectedCounter(null);
        }
    }
    private void SetSelectedCounter(BaseCounterView selectedCounter)
    {
        Context.SelectedCounter = selectedCounter;
        PlayerStateMachine.Instance.FireOnSelectedCounterChanged(new PlayerStateMachine.OnSelectedCounterChangedEventArgs
        {
            selectedCounterView = selectedCounter != null ? selectedCounter : null

        });

    }

}
