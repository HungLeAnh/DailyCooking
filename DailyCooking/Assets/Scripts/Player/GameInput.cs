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
    /*
    //public event EventHandler OnInteractAction;
    //public event EventHandler OnInteractAction2;
    //public event EventHandler OnPauseAction;
    //public event EventHandler OnBindingRebind;
    //public enum Binding
    //{
    //    Move_Up,
    //    Move_Down,
    //    Move_Left,
    //    Move_Right,
    //    Interact,
    //    Interact2,
    //    Pause,
    //    Gamepad_Interact,
    //    Gamepad_Interact2,
    //    Gamepad_Pause,
    //}

    //private PlayerAction _actions;

    //private void Awake()
    //{
    //    _actions = new PlayerAction();
    //    if (PlayerPrefs.HasKey(PLAYER_PREFS_BINDINGS))
    //    {
    //        _actions.LoadBindingOverridesFromJson(PlayerPrefs.GetString(PLAYER_PREFS_BINDINGS));
    //    }

    //    _actions.Player.Enable();
        
    //    _actions.Player.Interact.performed += Interact_performed;
    //    _actions.Player.Interact2.performed += Interact2_performed;
    //    _actions.Player.Pause.performed += Pause_performed;
    //}
    //private void OnDestroy()
    //{
    //    _actions.Player.Interact.performed -= Interact_performed;
    //    _actions.Player.Interact2.performed -= Interact2_performed;
    //    _actions.Player.Pause.performed -= Pause_performed;

    //    _actions.Dispose();
    //}
    //private void Pause_performed(InputAction.CallbackContext context)
    //{
    //    OnPauseAction?.Invoke(this,EventArgs.Empty);
    //}

    //private void Interact2_performed(InputAction.CallbackContext context)
    //{
    //    OnInteractAction2?.Invoke(this,EventArgs.Empty);
    //}

    //private void Interact_performed(InputAction.CallbackContext context)
    //{
    //    OnInteractAction?.Invoke(this,EventArgs.Empty);
    //}
    //public Vector2 GetMovementVectorNormalized()
    //{
    //    Vector2 inputVector = _actions.Player.Move.ReadValue<Vector2>();

    //    inputVector = inputVector.normalized;

    //    return inputVector;
    //}

    //public string GetBindingText(Binding binding)
    //{
    //    switch (binding)
    //    {
    //        default:
    //        case Binding.Move_Up:
    //            return _actions.Player.Move.bindings[1].ToDisplayString();

    //        case Binding.Move_Down:
    //            return _actions.Player.Move.bindings[2].ToDisplayString();

    //        case Binding.Move_Left:
    //            return _actions.Player.Move.bindings[3].ToDisplayString();

    //        case Binding.Move_Right:
    //            return _actions.Player.Move.bindings[4].ToDisplayString();

    //        case Binding.Interact:
    //            return _actions.Player.Interact.bindings[0].ToDisplayString();

    //        case Binding.Interact2:
    //            return _actions.Player.Interact2.bindings[0].ToDisplayString();

    //        case Binding.Pause:
    //            return _actions.Player.Pause.bindings[0].ToDisplayString();


    //        case Binding.Gamepad_Interact:
    //            return _actions.Player.Interact.bindings[1].ToDisplayString();

    //        case Binding.Gamepad_Interact2:
    //            return _actions.Player.Interact2.bindings[1].ToDisplayString();

    //        case Binding.Gamepad_Pause:
    //            return _actions.Player.Pause.bindings[1].ToDisplayString();

    //    }
    //}
    //public void RebindBinding(Binding binding, Action onActionRebound)
    //{
    //    _actions.Player.Disable();

    //    InputAction inputAction;
    //    int bindingIndex;

    //    switch (binding)
    //    {
    //        default:
    //        case Binding.Move_Up:
    //            inputAction = _actions.Player.Move;
    //            bindingIndex = 1;
    //            break;
    //        case Binding.Move_Down:
    //            inputAction = _actions.Player.Move;
    //            bindingIndex = 2;
    //            break;
    //        case Binding.Move_Left:
    //            inputAction = _actions.Player.Move;
    //            bindingIndex = 3;
    //            break;
    //        case Binding.Move_Right:
    //            inputAction = _actions.Player.Move;
    //            bindingIndex = 4;
    //            break;
    //        case Binding.Interact:
    //            inputAction = _actions.Player.Interact;
    //            bindingIndex = 0;
    //            break;
    //        case Binding.Interact2:
    //            inputAction = _actions.Player.Interact2;
    //            bindingIndex = 0;
    //            break;
    //        case Binding.Pause:
    //            inputAction = _actions.Player.Pause;
    //            bindingIndex = 0;
    //            break;
    //        case Binding.Gamepad_Interact:
    //            inputAction = _actions.Player.Interact;
    //            bindingIndex = 1;
    //            break;
    //        case Binding.Gamepad_Interact2:
    //            inputAction = _actions.Player.Interact2;
    //            bindingIndex = 1;
    //            break;
    //        case Binding.Gamepad_Pause:
    //            inputAction = _actions.Player.Pause;
    //            bindingIndex = 1;
    //            break;
    //    }

    //    inputAction.PerformInteractiveRebinding(bindingIndex)
    //        .OnComplete(callback => {
    //            callback.Dispose();
    //            _actions.Player.Enable();
    //            onActionRebound();

    //            PlayerPrefs.SetString(PLAYER_PREFS_BINDINGS, _actions.SaveBindingOverridesAsJson());
    //            PlayerPrefs.Save();

    //            OnBindingRebind?.Invoke(this, EventArgs.Empty);
    //        })
    //        .Start();
    //}
    */
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
        playerAction?.Player.Disable();
        playerAction?.Dispose();
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
        if (EnhancedTouch.Touch.activeFingers.Count == 0)
            return;
        if (EnhancedTouch.Touch.activeFingers.Count == 2)
        {
            OnPintchPerformed?.Invoke(this, EventArgs.Empty);
        }
        else
        {
            timeSinceLastTouch += Time.deltaTime;
            if (timeSinceLastTouch > touchTimeThreshold)
                isPanning = true;

            CheckMove();
            CheckTouch();

        }
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