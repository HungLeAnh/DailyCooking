using CodeMonkey.Utils;
using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class GameInput : PersistentSingleton<GameInput>
{
    private const string PLAYER_PREFS_BINDINGS = "InputBindings";
    [Obsolete("Use buildingGhostRayDistance (serialized) instead. Kept for backward compat.")]
    private const float INTERACT_DISTANCE_MAX = 999f;
    [SerializeField, Tooltip("Max raycast distance for BuildingGhost hit test. Former magic 999f extracted to inspector.")]
    private float buildingGhostRayDistance = 100f;
    
    public event EventHandler OnInteract1Performed;
    public event EventHandler OnInteract2Performed;

    public event EventHandler<Vector2> OnMousePanPerformed;
    public event EventHandler<Vector2> OnMouseClickPerformed;
    public event EventHandler OnMouseClickCanceled;
    public event EventHandler<float> OnScrollPerformed;
    // Pixels moved this frame by the finger (or right-mouse drag) that turns the walking camera.
    public event EventHandler<Vector2> OnLookDelta;
    private readonly LookTouchTracker lookTouchTracker = new LookTouchTracker();
    private readonly System.Collections.Generic.List<RaycastResult> uiRaycastResults = new System.Collections.Generic.List<RaycastResult>();
    private PointerEventData uiRaycastPointer;


    [SerializeField] private float touchTimeThreshold = 0.1f;
    [SerializeField] private float dragThreshold = 10f;
    [SerializeField] private LayerMask buildingGhostLayerMask;

    private PlayerAction playerAction;
    private bool isTouching = false;
    private bool isPanning = false;
    private float timeSinceLastTouch = 0f;
    private Vector2 lastClickPosition = Vector2.zero;
    private bool isTouchOverUI = false;
    private int touchCount = 0;
    private bool isTouchOverBuildingGhost = false;

    private float prevMagnitude = 0f;
    public PlayerAction PlayerAction { get => playerAction; }
    public Vector2 LastClickPosition { get => lastClickPosition; }
    public bool IsTouchOverBuildingGhost { get => isTouchOverBuildingGhost; }
    protected override void Awake()
    {
        base.Awake();
        playerAction = new PlayerAction();
        UnityEngine.InputSystem.EnhancedTouch.EnhancedTouchSupport.Enable();

        if (PlayerPrefs.HasKey(PLAYER_PREFS_BINDINGS))
        {
            playerAction.LoadBindingOverridesFromJson(PlayerPrefs.GetString(PLAYER_PREFS_BINDINGS));
        }

        playerAction.Player.Enable();
        playerAction.Player.Zoom.performed += Zoom_performed;
        playerAction.Player.Click.performed += ctx => StartClick();
        playerAction.Player.Click.canceled += ctx => EndClick();
        playerAction.Player.Pan.performed += ctx => PanCamera();
        playerAction.Player.Interact1.performed += ctx => Interact1Performed();
        playerAction.Player.Interact2.performed += ctx => Interact2Performed();
        //Touch
        playerAction.Player.Touch0Contact.performed += TouchContactPerformed;
        playerAction.Player.Touch1Contact.performed += TouchContactPerformed;
        playerAction.Player.Touch0Contact.canceled += ToucContactCanceled;
        playerAction.Player.Touch1Contact.canceled += ToucContactCanceled;

        playerAction.Player.Touch1Pos.performed += Touch1Pos_performed;


    }

    private void Interact2Performed()
    {
        OnInteract2Performed?.Invoke(this, EventArgs.Empty);
    }

    private void Interact1Performed()
    {
        OnInteract1Performed?.Invoke(this, EventArgs.Empty);
    }

    private void TouchContactPerformed(InputAction.CallbackContext obj)
    {
        if(isTouchOverUI)
            return;
        touchCount++;
    }

    private void ToucContactCanceled(InputAction.CallbackContext obj)
    {
        if(touchCount > 0)
            touchCount--; 
        prevMagnitude = 0;
    }
    private void Touch1Pos_performed(InputAction.CallbackContext obj)
    {
        if (touchCount < 2 || isTouchOverUI)
            return;

        var magnitude = (playerAction.Player.Touch0Pos.ReadValue<Vector2>() -
            playerAction.Player.Touch1Pos.ReadValue<Vector2>()).magnitude;
        if (prevMagnitude == 0)
            prevMagnitude = magnitude;
        var difference = magnitude - prevMagnitude;
        prevMagnitude = magnitude;

        OnScrollPerformed?.Invoke(this, difference);

    }

    //Bug not check on UI click yet on Mobile
    private void PanCamera()
    {
        if (isPanning)
        {
            lastClickPosition = playerAction.Player.Pan.ReadValue<Vector2>();
            OnMousePanPerformed?.Invoke(this, lastClickPosition);
            //Debug.Log("panning... " + lastClickPosition);
        }
    }

    private void Zoom_performed(InputAction.CallbackContext obj)
    {
        //Debug.LogError("Zoom: " + obj.ReadValue<Vector2>());
        OnScrollPerformed?.Invoke(this, obj.ReadValue<Vector2>().y);
    }
    private void StartClick()
    {
        if (IsMouseOverUI())
        {
            isTouchOverUI = true;
            return;
        }
        timeSinceLastTouch = 0f;
        lastClickPosition = playerAction.Player.Pan.ReadValue<Vector2>();
        isTouching = true;
        isPanning = false;
        isTouchOverBuildingGhost = CheckTouchOverBuildingGhost(lastClickPosition);
        OnMouseClickPerformed?.Invoke(this, lastClickPosition);
    }

    private void EndClick()
    {
        timeSinceLastTouch = 0f;
        lastClickPosition = Vector2.zero;
        isTouching = false;
        isPanning = false;
        isTouchOverUI = false;
        isTouchOverBuildingGhost = false;
        OnMouseClickCanceled?.Invoke(this, EventArgs.Empty);
    }
    private void Update()
    {
        HandleLook();
        if (IsMouseOverUI())
        {
            isTouchOverUI = true;
            return;
        }

        if (!isTouching || isTouchOverUI)
            return;

        if(isTouchOverBuildingGhost)
            return;

        timeSinceLastTouch += Time.deltaTime;
        if (timeSinceLastTouch > touchTimeThreshold)
            isPanning = true;
    }
    
    // The look finger is the first touch that did not start on UI, so the joystick thumb never turns
    // the camera. Without a touchscreen, a right-mouse drag does the same.
    private void HandleLook()
    {
        Vector2 delta = Vector2.zero;
        if (Touchscreen.current != null)
        {
            foreach (var touch in UnityEngine.InputSystem.EnhancedTouch.Touch.activeTouches)
            {
                int id = touch.touchId;
                switch (touch.phase)
                {
                    case UnityEngine.InputSystem.TouchPhase.Began:
                        lookTouchTracker.Began(id, touch.screenPosition, IsScreenPointOverUI(touch.screenPosition));
                        break;
                    case UnityEngine.InputSystem.TouchPhase.Moved:
                        delta += lookTouchTracker.Moved(id, touch.screenPosition);
                        break;
                    case UnityEngine.InputSystem.TouchPhase.Ended:
                    case UnityEngine.InputSystem.TouchPhase.Canceled:
                        lookTouchTracker.Ended(id);
                        break;
                }
            }
        }
        else if (Mouse.current != null && Mouse.current.rightButton.isPressed)
        {
            delta = Mouse.current.delta.ReadValue();
        }
        if (delta != Vector2.zero)
            OnLookDelta?.Invoke(this, delta);
    }

    // Raycasts the UI directly instead of IsPointerOverGameObject(touchId): that reads the input
    // module's per-pointer state, which is only filled in during EventSystem.Update, so on a touch's
    // first frame it depends on script order whether the joystick thumb counts as "over UI".
    public bool IsScreenPointOverUI(Vector2 screenPosition)
    {
        EventSystem eventSystem = EventSystem.current;
        if (eventSystem == null)
            return false;
        uiRaycastPointer ??= new PointerEventData(eventSystem);
        uiRaycastPointer.Reset();
        uiRaycastPointer.position = screenPosition;
        uiRaycastResults.Clear();
        eventSystem.RaycastAll(uiRaycastPointer, uiRaycastResults);
        return uiRaycastResults.Count > 0;
    }

    public bool IsMouseOverUI()
    {
        if (EventSystem.current == null)
            return false;

        if(Pointer.current != null)
            return EventSystem.current.IsPointerOverGameObject();

        var touchscreen = Touchscreen.current;
        if (touchscreen == null)
            return false;

        foreach (var touch in touchscreen.touches)
        {
            if (touch.phase.ReadValue() == UnityEngine.InputSystem.TouchPhase.Began)
            {
                int touchId = touch.touchId.ReadValue();
                if (EventSystem.current.IsPointerOverGameObject(touchId))
                {
                    return true;
                }
            }
        }
        return false;
    }
    public bool CheckTouchOverBuildingGhost(Vector2 position)
    {
        float interactDistance = buildingGhostRayDistance;
        Ray ray = UtilsClass.MainCamera.ScreenPointToRay(position);
        if (Physics.Raycast(ray, out RaycastHit raycastHit, interactDistance, buildingGhostLayerMask))
        {
            //Debug.Log("Touch Position: " + raycastHit.transform.position);
            return true;
        }
        return false;
    }
    public Vector2 GetMovementVectorNormalized()
    {
        Vector2 inputVector = playerAction.Player.Moving.ReadValue<Vector2>();

        inputVector = inputVector.normalized;

        return inputVector;
    }
    public Vector2 GetClickPosition()
    {
        return playerAction.Player.Pan.ReadValue<Vector2>();
    }
}