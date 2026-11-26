using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;
using TouchPhase = UnityEngine.InputSystem.TouchPhase;

// One pointer (first finger, or the left mouse button) for the local player's cooking-tool close-up.
// A press on the held-item slot is a drag-in; a press near the food in the tool that is released on
// the slot is a drag-out; every other press is forwarded to the gestures (Down/Drag/Up).
public class CloseUpPointer : MonoBehaviour
{
    // Screen-space distance (fraction of screen height) at which a press counts as "on the food".
    private const float FoodPickRadius = 0.12f;

    [SerializeField] private RectTransform heldItemSlot;
    [SerializeField] private UnityEngine.UI.Image dragGhost;

    public event Action<Vector2> OnDown;
    public event Action<Vector2> OnDrag;
    public event Action<Vector2> OnUp;

    private enum PressKind { None, SlotDrag, Gesture }

    private PressKind press = PressKind.None;
    private int touchId = -1;
    private bool pressStartedOnFood;
    private Vector2 pressStart;

    private void OnEnable()
    {
        EnhancedTouchSupport.Enable();
    }

    private void Update()
    {
        CameraModeController cameraMode = CameraModeController.Instance;
        if (cameraMode == null || cameraMode.Mode != CameraMode.CloseUp || cameraMode.CloseUpTool == null)
        {
            Cancel();
            return;
        }

        // Touch first; the mouse is polled too (touch laptops, Device Simulator) whenever no
        // finger owns the press.
        if (Touchscreen.current != null)
            PollTouch();
        if (Mouse.current != null && touchId < 0 && Touch.activeTouches.Count == 0)
            PollMouse();
    }

    private void PollTouch()
    {
        foreach (Touch touch in Touch.activeTouches)
        {
            if (press == PressKind.None && touch.phase == TouchPhase.Began)
            {
                if (Begin(touch.screenPosition))
                    touchId = touch.touchId;
                continue;
            }
            if (touch.touchId != touchId)
                continue;
            if (touch.phase == TouchPhase.Moved || touch.phase == TouchPhase.Stationary)
                Move(touch.screenPosition);
            else if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
                End(touch.screenPosition);
        }
    }

    private void PollMouse()
    {
        var button = Mouse.current.leftButton;
        Vector2 position = Mouse.current.position.ReadValue();
        if (button.wasPressedThisFrame && press == PressKind.None)
        {
            Begin(position);
            // A click can press and release within one frame: end it here too.
            if (!button.isPressed && press != PressKind.None)
                End(position);
            return;
        }
        if (press == PressKind.None)
            return;
        if (button.isPressed)
            Move(position);
        else
            End(position);
    }

    private bool Begin(Vector2 position)
    {
        pressStart = position;
        if (IsOverSlot(position))
        {
            KitchenObject held = LocalPlayer != null ? LocalPlayer.GetKitchenObject() : null;
            if (held == null)
                return false;
            press = PressKind.SlotDrag;
            ShowGhost(held.GetKitchenObjectSO(), position);
            return true;
        }
        if (IsOverScreenUI(position))
            return false;
        press = PressKind.Gesture;
        pressStartedOnFood = IsNearFood(position, out _);
        OnDown?.Invoke(position);
        return true;
    }

    private void Move(Vector2 position)
    {
        if (press == PressKind.SlotDrag)
        {
            MoveGhost(position);
            return;
        }
        // The food ghost only shows while a drag from the food heads for the slot, so cuts and
        // stirs that start on the food do not look like take-outs.
        if (pressStartedOnFood && IsNearSlot(position) && IsNearFood(pressStart, out KitchenObject food))
            ShowGhost(food.GetKitchenObjectSO(), position);
        else if (press == PressKind.Gesture && dragGhost != null)
            dragGhost.gameObject.SetActive(false);
        OnDrag?.Invoke(position);
    }

    private void End(Vector2 position)
    {
        PressKind ended = press;
        bool startedOnFood = pressStartedOnFood;
        Cancel();
        CookingToolCounterController controller = CurrentController;
        if (controller == null || LocalPlayer == null)
            return;

        // Released on a HUD button (BACK, joystick...): never drop or take anything there, but a
        // running gesture (e.g. a pour hold) still ends.
        if (!IsOverSlot(position) && IsOverScreenUI(position))
        {
            if (ended == PressKind.Gesture)
                OnUp?.Invoke(position);
            return;
        }

        if (ended == PressKind.SlotDrag)
        {
            // Drag in: released over the tool (or its counter).
            if (IsOverTool(position))
                LocalPlayer.RequestInteractFromCloseUp(controller);
            return;
        }
        if (startedOnFood && IsOverSlot(position))
        {
            // Drag out: from the food onto the held-item slot (hand, plate or cup).
            LocalPlayer.RequestInteractFromCloseUp(controller);
            return;
        }
        OnUp?.Invoke(position);
    }

    private void Cancel()
    {
        press = PressKind.None;
        touchId = -1;
        pressStartedOnFood = false;
        if (dragGhost != null)
            dragGhost.gameObject.SetActive(false);
    }

    private static PlayerStateMachine LocalPlayer => PlayerStateMachine.LocalInstance;

    private readonly System.Collections.Generic.List<UnityEngine.EventSystems.RaycastResult> uiHits =
        new System.Collections.Generic.List<UnityEngine.EventSystems.RaycastResult>();

    // HUD buttons block a press; the tool's own world-space widgets (progress bar, burn warning)
    // sit right over the food in the close-up and must not swallow gestures.
    private bool IsOverScreenUI(Vector2 position)
    {
        var eventSystem = UnityEngine.EventSystems.EventSystem.current;
        if (eventSystem == null)
            return false;
        uiHits.Clear();
        eventSystem.RaycastAll(new UnityEngine.EventSystems.PointerEventData(eventSystem) { position = position }, uiHits);
        foreach (var hit in uiHits)
        {
            Canvas canvas = hit.gameObject.GetComponentInParent<Canvas>();
            if (canvas == null || canvas.rootCanvas.renderMode != RenderMode.WorldSpace)
                return true;
        }
        return false;
    }

    private static CookingToolCounterController CurrentController
    {
        get
        {
            CameraModeController cameraMode = CameraModeController.Instance;
            CookingTool tool = cameraMode != null ? cameraMode.CloseUpTool : null;
            return tool != null ? CookingToolCounterController.FindControllerForTool(tool) : null;
        }
    }

    private bool IsOverSlot(Vector2 position)
    {
        return heldItemSlot != null && heldItemSlot.gameObject.activeInHierarchy &&
               RectTransformUtility.RectangleContainsScreenPoint(heldItemSlot, position, null);
    }

    private bool IsNearSlot(Vector2 position)
    {
        if (IsOverSlot(position))
            return true;
        if (heldItemSlot == null || !heldItemSlot.gameObject.activeInHierarchy)
            return false;
        Vector2 slotCentre = RectTransformUtility.WorldToScreenPoint(null, heldItemSlot.position);
        return Vector2.Distance(position, slotCentre) <= Screen.height * 0.25f;
    }

    private static bool IsNearFood(Vector2 position, out KitchenObject food)
    {
        food = null;
        CookingTool tool = CameraModeController.Instance != null ? CameraModeController.Instance.CloseUpTool : null;
        Camera camera = Camera.main;
        if (tool == null || camera == null || !tool.TryGetFirstFood(out food, out _))
            return false;
        Vector3 screen = camera.WorldToScreenPoint(food.transform.position);
        return screen.z > 0f && Vector2.Distance(position, screen) <= Screen.height * FoodPickRadius;
    }

    private static bool IsOverTool(Vector2 position)
    {
        CameraModeController cameraMode = CameraModeController.Instance;
        Camera camera = Camera.main;
        if (cameraMode == null || cameraMode.CloseUpTool == null || camera == null)
            return false;
        CookingToolCounterController controller = CookingToolCounterController.FindControllerForTool(cameraMode.CloseUpTool);
        Ray ray = camera.ScreenPointToRay(position);
        foreach (RaycastHit hit in Physics.RaycastAll(ray, 50f, ~0, QueryTriggerInteraction.Ignore))
        {
            if (hit.transform.IsChildOf(cameraMode.CloseUpTool.transform) ||
                (controller != null && hit.transform.IsChildOf(controller.transform)))
                return true;
        }
        // Fall back to "near the tool on screen" when the tool's colliders miss the ray.
        Vector3 toolScreen = camera.WorldToScreenPoint(cameraMode.CloseUpLookAt);
        return toolScreen.z > 0f && Vector2.Distance(position, toolScreen) <= Screen.height * 0.25f;
    }

    private void ShowGhost(KitchenObjectSO so, Vector2 position)
    {
        if (dragGhost == null || so == null)
            return;
        dragGhost.sprite = so.Sprite;
        dragGhost.gameObject.SetActive(true);
        MoveGhost(position);
    }

    private void MoveGhost(Vector2 position)
    {
        if (dragGhost != null && dragGhost.gameObject.activeSelf)
            dragGhost.rectTransform.position = position;
    }
}
