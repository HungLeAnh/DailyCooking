using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.AI;

public class PlayerStateMachine : PersistentSingleton<PlayerStateMachine>, IKitchenObjectParent
{
    public PlayerStateContext Context { get; set; }

    public event EventHandler OnPickedSomething;

    public enum EPlayerState
    {
        // Top-level states
        Idle, 
        Holding,

        // Sub-states for Idle
        Idle_Idle, 
        Idle_Walking,

        // Sub-states for Holding
        Holding_Idle,
        Holding_Walking,
    }

    [SerializeField]
    private float moveSpeed = 7f;
    [SerializeField]
    private LayerMask countersLayerMask;
    [SerializeField] 
    private Transform kitchenObjectHoldPoint;
    [SerializeField] 
    private Animator characterAnimator;
    [SerializeField]
    private NavMeshAgent navMeshAgent;

    private StateManager<EPlayerState> _stateManager;
    private PlayerStateFactory _stateFactory;

    private void IntializeStates()
    {
        Context = new PlayerStateContext(characterAnimator, moveSpeed,
            this.transform, countersLayerMask, this.kitchenObjectHoldPoint, navMeshAgent);

        var states = _stateFactory.CreateStates(this);
        _stateManager.SetStates(states, EPlayerState.Idle);
        _stateManager.Start();

        SetupNavMeshAgent();
    }

    private void SetupNavMeshAgent()
    {
        navMeshAgent.speed = moveSpeed;
    }

    protected override void Awake()
    {
        base.Awake();
        _stateManager = new StateManager<EPlayerState>();
        _stateFactory = new PlayerStateFactory();
        IntializeStates();
        Context.PlayerGameInput.OnTouchPerformed += PlayerGameInput_OnFingerDown;
        Context.PlayerGameInput.OnFingerUp += PlayerGameInput_OnFingerUp;
    }
    private void OnDestroy()
    {
        Context.PlayerGameInput.OnTouchPerformed -= PlayerGameInput_OnFingerDown;
        Context.PlayerGameInput.OnFingerUp -= PlayerGameInput_OnFingerUp;
        Context = null;
        _stateManager.Dispose();
    }
    private void Update()
    {
        _stateManager.Update();
    }

    public Transform GetKitchenObjectFollowTransform()
    {
        return kitchenObjectHoldPoint;
    }

    public void SetKitchenObject(KitchenObject kitchenObject)
    {
        Context.KitchenObject = kitchenObject;

        if (kitchenObject != null)
        {
            OnPickedSomething?.Invoke(this, EventArgs.Empty);
        }
    }

    public KitchenObject GetKitchenObject()
    {
        return Context.KitchenObject;
    }

    public void ClearKitchenObject()
    {
        Context.KitchenObject = null;
    }

    public bool HasKitchenObject()
    {
        return Context.KitchenObject != null;
    }
    public void DisableInput(bool isDisable)
    {
        Context.IsDisableInput = isDisable;
    }

    private void PlayerGameInput_OnFingerUp(object sender, UnityEngine.InputSystem.EnhancedTouch.Finger e)
    {
        Context.IsTouching = false;

    }

    private void PlayerGameInput_OnFingerDown(object sender, UnityEngine.InputSystem.EnhancedTouch.Finger finger)
    {
        if (Context.IsDisableInput)
            return;
        if (Context.IsTouching)
            return;
        if (!KitchenGameManager.Instance.IsGamePlaying())
            return;
        if (finger.currentTouch.delta.sqrMagnitude >= 0.1f)
            return;

        Context.IsTouching = true;
        float interactDistance = 999f;
        if (!Camera.main.pixelRect.Contains(finger.screenPosition))
            return;
        Ray ray = Camera.main.ScreenPointToRay(finger.screenPosition);
        if (Physics.Raycast(ray, out RaycastHit raycastHit, interactDistance, Context.CounterLayerMask))
        {
            if (raycastHit.transform.TryGetComponent(out BaseCounterController baseCounter))
            {
                if (baseCounter != Context.SelectedCounterController)
                {
                    SetSelectedCounter(baseCounter);
                    if (baseCounter.gameObject.TryGetComponent(out PlacedObjectView placedObjectView))
                    {
                        int2 gridPos = new int2(placedObjectView.GetModel().Origin.x, placedObjectView.GetModel().Origin.y);
                        Vector3 counterOrigin = GridBuildingSystem.Instance.GridManager
                                    .GridPositionToWorldPosition(gridPos);
                        MoveToPosition(counterOrigin);
                    }
                }
                else if (baseCounter == Context.SelectedCounterController)
                {
                    if (Context.SelectedCounterController != null &&
                        CounterModules.Instance.IsContainerCounter(Context.SelectedCounterController))
                    {
                        IHasProgress progress = Context.SelectedCounterController as IHasProgress;
                        if (progress == null)
                        {
                            if (navMeshAgent.remainingDistance <= navMeshAgent.stoppingDistance)
                            {
                                SetSelectedCounter(baseCounter);
                                Context.SelectedCounterController.InteractEvent(PlayerStateMachine.Instance);
                            }
                        }
                        else
                        {

                            if (progress.IsDone())
                            {
                                Context.SelectedCounterController.InteractEvent(PlayerStateMachine.Instance);

                            }
                            else
                            {

                                Context.SelectedCounterController.InteractAlternateEvent(PlayerStateMachine.Instance);
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

    private void MoveToPosition(Vector3 counterOrigin)
    {
        navMeshAgent.SetDestination(counterOrigin);
        Context.IsReachedDestination = false;
        Context.IsWalking = true;
        Context.NavMeshAgent.isStopped = false;

    }

    private void SetSelectedCounter(BaseCounterController selectedCounter)
    {
        Context.SelectedCounterController = selectedCounter;
        CounterModules.Instance.FireOnSelectedCounterChanged(new CounterModules.OnSelectedCounterChangedEventArgs
        {
            selectedCounterController = selectedCounter != null ? selectedCounter : null

        });

    }
}
