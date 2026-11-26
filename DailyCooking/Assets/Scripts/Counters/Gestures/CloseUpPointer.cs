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

        if (Touchscreen.current != null)
            PollTouch();
        else if (Mouse.current != null)
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
        Vector2 position = Mouse.current.position.ReadValue();
        if (Mouse.current.leftButton.wasPressedThisFrame && press == PressKind.None)
            Begin(position);
        else if (Mouse.current.leftButton.isPressed && press != PressKind.None)
            Move(position);
        else if (Mouse.current.leftButton.wasReleasedThisFrame && press != PressKind.None)
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
        if (GameInput.Instance != null && GameInput.Instance.IsScreenPointOverUI(position))
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
        if (pressStartedOnFood && IsNearFood(pressStart, out KitchenObject food) &&
            Vector2.Distance(position, pressStart) > Screen.height * FoodPickRadius)
            ShowGhost(food.GetKitchenObjectSO(), position);
        else
            MoveGhost(position);
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
