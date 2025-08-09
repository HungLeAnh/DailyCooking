using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using EnhancedTouch = UnityEngine.InputSystem.EnhancedTouch;


public class GameInput : PersistentSingleton<GameInput>
{
    private const string PLAYER_PREFS_BINDINGS = "InputBindings";
    
    public event EventHandler<Finger> OnFingerDown;
    public event EventHandler<Finger> OnFingerMoved;
    public event EventHandler<Finger> OnFingerUp;
    public event EventHandler<Finger> OnTouchPerformed;
    public event EventHandler<Finger> OnDragPerformed;
    public event EventHandler OnPintchPerformed;


    [SerializeField] private float touchTimeThreshold = 0.1f;
    [SerializeField] private float dragThreshold = 10f;
    [SerializeField] private LayerMask buildingGhostLayerMask;

    private PlayerAction playerAction;
    private bool isTouching = false;
    private bool isPanning = false;
    private float timeSinceLastTouch = 0f;
    private Vector2 lastTouchPosition = Vector2.zero;

    public PlayerAction PlayerAction { get => playerAction; }

    protected override void Awake()
    {
        base.Awake();
        playerAction = new PlayerAction();
        playerAction.Player.Enable();
        EnhancedTouchSupport.Enable();
        TouchSimulation.Enable();


        EnhancedTouch.Touch.onFingerDown += Touch_OnFingerDown;
        EnhancedTouch.Touch.onFingerMove += Touch_OnFingerMoved;
        EnhancedTouch.Touch.onFingerUp += Touch_OnFingerUp;

    }

    private void OnDestroy()
    {
        //playerAction?.Player.Disable();
        //playerAction?.Dispose();
        //EnhancedTouchSupport.Enable();
        //EnhancedTouch.Touch.onFingerMove -= Touch_OnFingerMoved;
        //EnhancedTouch.Touch.onFingerUp -= Touch_OnFingerUp;
        //EnhancedTouch.Touch.onFingerDown -= Touch_OnFingerDown;

        //EnhancedTouchSupport.Disable();
        //TouchSimulation.Disable();

    }

    private void Touch_OnFingerUp(Finger finger)
    {
        timeSinceLastTouch = 0f;
        lastTouchPosition = Vector2.zero;
        isTouching = false;
        isPanning = false;
        OnFingerUp?.Invoke(this, finger);
    }

    private void Touch_OnFingerMoved(Finger finger)
    {
        isPanning = true;
        OnFingerMoved?.Invoke(this, finger);
    }

    private void Touch_OnFingerDown(Finger finger)
    {
        if (IsMouseOverUI())
            return;
        timeSinceLastTouch = 0f;
        lastTouchPosition = finger.screenPosition;
        isTouching = true;
        OnFingerDown?.Invoke(this, finger);
    }
    private void Update()
    {
        if (IsMouseOverUI())
            return;
        if (!isTouching)
            return;

        HandleTouch();
    }

    private void HandleTouch()
    {
        if (EnhancedTouch.Touch.activeFingers.Count == 0)
            return;

        if (EnhancedTouch.Touch.activeFingers.Count == 2)
        {
            HandlePinch();
        }
        else
        {
            HandlePanAndTap();
        }
    }

    private void HandlePinch()
    {
        OnPintchPerformed?.Invoke(this, EventArgs.Empty);
    }

    private void HandlePanAndTap()
    {
        timeSinceLastTouch += Time.deltaTime;
        if (timeSinceLastTouch > touchTimeThreshold)
            isPanning = true;

        CheckMove();
        CheckTouch();
    }

    private void CheckMove()
    {
        float distance = Vector2.Distance(lastTouchPosition, EnhancedTouch.Touch.activeFingers[0].screenPosition);
        if (distance > dragThreshold)
        {
            OnDragPerformed?.Invoke(this, EnhancedTouch.Touch.activeFingers[0]);
        }
    }

    private void CheckTouch()
    {
        if (isPanning)
            return;
        if (timeSinceLastTouch > touchTimeThreshold)
            return;


        float distance = Vector2.Distance(lastTouchPosition, EnhancedTouch.Touch.activeFingers[0].screenPosition);
        if (distance > dragThreshold)
            return;
        OnTouchPerformed?.Invoke(this, EnhancedTouch.Touch.activeFingers[0]);
    }

    public bool IsMouseOverUI()
    {
        return EventSystem.current.IsPointerOverGameObject();
    }
    public bool IsTouchOverBuildingGhost(Finger finger)
    {
        bool isTouchOverBuildingGhost = false;
        if (finger != null)
        {
            float interactDistance = 999f;
            Ray ray = Camera.main.ScreenPointToRay(finger.screenPosition);
            if (Physics.Raycast(ray, out RaycastHit raycastHit, interactDistance, buildingGhostLayerMask))
            {
                //Debug.Log("Touch Position: " + pos);
                if (raycastHit.transform.GetComponentInParent<BuildingGhost>() != null)
                {
                    isTouchOverBuildingGhost = true;
                }
                else
                {
                    isTouchOverBuildingGhost = false;
                }
            }

        }
        return isTouchOverBuildingGhost || EventSystem.current.IsPointerOverGameObject();
    }
}