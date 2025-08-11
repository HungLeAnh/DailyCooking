using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem.EnhancedTouch;

public abstract class PlayerBaseState : BaseState<PlayerStateMachine.EPlayerState>
{
    private const float MIN_DISTANCE_TO_TARGET = 0.05f;
    private const float INTERACT_DISTANCE_MAX = 999f;

    protected PlayerStateContext Context;
    public PlayerBaseState(PlayerStateMachine.EPlayerState stateKey) : base(stateKey)
    {
    }

    public virtual void IntializeStates(PlayerStateContext context)
    {
        Context = context;
        Context.PlayerGameInput.OnTouchPerformed += PlayerGameInput_OnFingerDown;
        Context.PlayerGameInput.OnFingerUp += PlayerGameInput_OnFingerUp;
    }

    public override void Dispose()
    {
        Context.PlayerGameInput.OnTouchPerformed -= PlayerGameInput_OnFingerDown;
        Context.PlayerGameInput.OnFingerUp -= PlayerGameInput_OnFingerUp;
        Context = null;
    }
    public virtual void ChangeAnimationState(string animationName)
    {
        if(Context != null)
        {
            Context.CharacterAnimator.CrossFade(animationName,0.1f,0,0.1f,0.1f);

        }
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

            if (Vector3.Distance(Context.PlayerTransform.position, nextTarget) < MIN_DISTANCE_TO_TARGET)
            {
                Context.WayPointIndex++;
                if (Context.WayPointIndex >= Context.PathList.Count)
                {
                    Context.IsWalking = false;
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
    public override void UpdateState()
    {
        if (!Context.IsDisableInput && Context.IsWalking)
        {
            UpdatePlayerPosition();
            if (Vector3.Distance(Context.PlayerTransform.position, Context.EndPosition) > MIN_DISTANCE_TO_TARGET)
                return;
            OnReachDestination();

        }
    }
    
    private void OnReachDestination()
    {
        Context.IsReachedDestination = true;
        if (Context.SelectedCounter != null)
        {
            Context.SelectedCounter.FireInteractEvent(PlayerStateMachine.Instance);
        }
    }
    private void PlayerGameInput_OnFingerUp(object sender, Finger e)
    {
        Context.IsTouching = false;
    }
    private void PlayerGameInput_OnFingerDown(object sender, Finger finger)
    {
        if (Context.IsTouching)
            return;
        if(!KitchenGameManager.Instance.IsGamePlaying())
            return;
        if (finger.currentTouch.delta.sqrMagnitude >= 0.1f)
            return;

        Context.IsTouching = true;
        float interactDistance = INTERACT_DISTANCE_MAX;
        if (!Camera.main.pixelRect.Contains(finger.screenPosition))
            return;
        Ray ray = Camera.main.ScreenPointToRay(finger.screenPosition);
        if (Physics.Raycast(ray, out RaycastHit raycastHit, interactDistance, Context.CounterLayerMask))
        {
            if (raycastHit.transform.TryGetComponent(out BaseCounterView baseCounter))
            {
                if (baseCounter != Context.SelectedCounter)
                {
                    SetSelectedCounter(baseCounter);
                    if (baseCounter.gameObject.TryGetComponent(out PlacedObjectView placedObjectView))
                    {
                        int2 counterOrigin = new int2(placedObjectView.GetModel().Origin.x, placedObjectView.GetModel().Origin.y);
                        int2 playerPos = GridBuildingSystem.Instance.WorldPositionToGridPos(Context.PlayerTransform.position.x, Context.PlayerTransform.position.z);
                        Context.IsReachedDestination = false;
                        GridBuildingSystem.Instance.FindPath(playerPos, counterOrigin);

                    }
                }
                else if (baseCounter == Context.SelectedCounter)
                {
                    if (Context.SelectedCounter != null && 
                        CounterModules.Instance.TryGetCounterController(Context.SelectedCounter,
                                                out BaseCounterController baseCounterController))
                    {
                        IHasProgress progress = baseCounterController as IHasProgress;
                        if (progress == null)
                        {
                            if (Context.IsReachedDestination)
                            {
                                SetSelectedCounter(baseCounter);
                                Context.SelectedCounter.FireInteractEvent(PlayerStateMachine.Instance);
                            }
                        }
                        else
                        {

                            if (progress.IsDone())
                            {
                                Context.SelectedCounter.FireInteractEvent(PlayerStateMachine.Instance);

                            }
                            else
                            {

                                Context.SelectedCounter.FireInteractAlternateEvent(PlayerStateMachine.Instance);
                            }
                        }
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
        CounterModules.Instance.FireOnSelectedCounterChanged(new CounterModules.OnSelectedCounterChangedEventArgs
        {
            selectedCounterView = selectedCounter != null ? selectedCounter : null

        });

    }

}
