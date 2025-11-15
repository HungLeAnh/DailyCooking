using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class GameInput : PersistentSingleton<GameInput>
{
    private const string PLAYER_PREFS_BINDINGS = "InputBindings";
    private const float INTERACT_DISTANCE_MAX = 999f;
    
    public event EventHandler OnInteract1Performed;
    public event EventHandler OnInteract2Performed;

    public event EventHandler<Vector2> OnMousePanPerformed;
    public event EventHandler<Vector2> OnMouseClickPerformed;
    public event EventHandler OnMouseClickCanceled;
    public event EventHandler<float> OnScrollPerformed;


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

    private float prevMagnitude = 0f;
    public PlayerAction PlayerAction { get => playerAction; }

    protected override void Awake()
    {
        base.Awake();
        playerAction = new PlayerAction();

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

        OnMouseClickPerformed?.Invoke(this, lastClickPosition);
    }

    private void EndClick()
    {
        timeSinceLastTouch = 0f;
        lastClickPosition = Vector2.zero;
        isTouching = false;
        isPanning = false;
        isTouchOverUI = false;

        OnMouseClickCanceled?.Invoke(this, EventArgs.Empty);
    }
    private void Update()
    {
        if (IsMouseOverUI())
        {
            isTouchOverUI = true;
            return;
        }

        if (!isTouching || isTouchOverUI)
            return;

        timeSinceLastTouch += Time.deltaTime;
        if (timeSinceLastTouch > touchTimeThreshold)
            isPanning = true;
    }
    
    public bool IsMouseOverUI()
    {
        if(Touchscreen.current == null)
            return EventSystem.current.IsPointerOverGameObject();

        foreach (var touch in Touchscreen.current.touches)
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
    public bool IsTouchOverBuildingGhost(Vector2 position)
    {
        bool isTouchOverBuildingGhost = false;
        
        float interactDistance = INTERACT_DISTANCE_MAX;
        Ray ray = Camera.main.ScreenPointToRay(position);
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

        return isTouchOverBuildingGhost || EventSystem.current.IsPointerOverGameObject();
    }
    public Vector2 GetMovementVectorNormalized()
    {
        Vector2 inputVector = playerAction.Player.Moving.ReadValue<Vector2>();

        inputVector = inputVector.normalized;

        return inputVector;
    }
}