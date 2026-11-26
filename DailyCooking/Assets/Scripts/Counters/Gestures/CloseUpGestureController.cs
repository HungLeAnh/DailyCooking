using UnityEngine;

// Local player's gestures in a cooking-tool close-up. Reads CloseUpPointer, recognises the tool's
// gesture family, shows the prompt and reports each finished gesture to the server.
public class CloseUpGestureController : MonoBehaviour
{
    // Food size on screen for cut scoring, and the flick that counts as a flip (fractions of screen height).
    private const float FoodScreenRadius = 0.09f;
    private const float FlipMinRise = 0.08f;
    private const float FlipMaxSeconds = 0.3f;

    [SerializeField] private CloseUpPointer pointer;
    [SerializeField] private GesturePromptUI prompt;

    private readonly GestureRecognizers.StirTracker stirTracker = new GestureRecognizers.StirTracker();

    private CookingTool tool;
    private CookingTool.State lastState;
    private Vector2 downPosition;
    private float downTime;
    private bool flippedThisCook;
    private int lastStirPrompt = -1;
    private bool pouring;
    private bool pouredThisCook;
    private float pourStart;

    private void OnEnable()
    {
        if (pointer == null)
            return;
        pointer.OnDown += Pointer_OnDown;
        pointer.OnDrag += Pointer_OnDrag;
        pointer.OnUp += Pointer_OnUp;
    }

    private void OnDisable()
    {
        if (pointer == null)
            return;
        pointer.OnDown -= Pointer_OnDown;
        pointer.OnDrag -= Pointer_OnDrag;
        pointer.OnUp -= Pointer_OnUp;
    }

    private void Update()
    {
        CameraModeController cameraMode = CameraModeController.Instance;
        CookingTool current = cameraMode != null && cameraMode.Mode == CameraMode.CloseUp ? cameraMode.CloseUpTool : null;
        if (current != tool)
            ResetCook(current);
        else if (current != null && current.CurrentState != lastState)
            OnStateChanged(current.CurrentState);
        if (tool == null)
        {
            if (prompt != null) prompt.Hide();
            return;
        }
        UpdatePrompt();
    }

    private void ResetCook(CookingTool current)
    {
        tool = current;
        lastState = current != null ? current.CurrentState : CookingTool.State.Idle;
        flippedThisCook = false;
        lastStirPrompt = -1;
        pouring = false;
        pouredThisCook = false;
    }

    // A new cook starts when the tool leaves Idle; the hold that started a manual-start pour
    // survives that change. Back to Idle (food taken out / new input) forgets everything.
    private void OnStateChanged(CookingTool.State state)
    {
        CookingTool.State previous = lastState;
        lastState = state;
        if (state == CookingTool.State.Idle)
        {
            ResetCook(tool);
            return;
        }
        if (previous == CookingTool.State.Idle && state == CookingTool.State.Cooking)
        {
            flippedThisCook = false;
            lastStirPrompt = -1;
        }
        // The cook is over: a hold still running can no longer score (the server only takes
        // pours while cooking), so drop it instead of showing a bar that keeps filling.
        if (previous == CookingTool.State.Cooking && pouring)
        {
            pouring = false;
            pouredThisCook = true;
        }
    }

    private CookingToolCounterController Controller =>
        tool != null ? CookingToolCounterController.FindControllerForTool(tool) : null;

    private void Report(GestureFamily family, float value)
    {
        PlayerStateMachine player = PlayerStateMachine.LocalInstance;
        if (player != null)
            player.ReportGesture(Controller, family, value);
    }

    private void Pointer_OnDown(Vector2 position)
    {
        if (tool == null)
            return;
        downPosition = position;
        downTime = Time.unscaledTime;
        switch (tool.GestureFamily)
        {
            case GestureFamily.Stir:
                stirTracker.Reset(ScreenPoint(CameraModeController.Instance.CloseUpLookAt));
                stirTracker.Feed(position);
                break;
            case GestureFamily.Pour:
                BeginPour();
                break;
        }
    }

    private void Pointer_OnDrag(Vector2 position)
    {
        if (tool == null || tool.GestureFamily != GestureFamily.Stir || tool.CurrentState != CookingTool.State.Cooking)
            return;
        if (!stirTracker.Feed(position))
            return;
        int promptIndex = tool.CurrentStirPromptIndex;
        if (promptIndex == lastStirPrompt)
            return;
        lastStirPrompt = promptIndex;
        Report(GestureFamily.Stir, promptIndex);
    }

    private void Pointer_OnUp(Vector2 position)
    {
        if (tool == null)
            return;
        switch (tool.GestureFamily)
        {
            case GestureFamily.Cut:
                if (!tool.HasAnyFood || !tool.IsCuttingActive || !tool.TryGetFirstFood(out KitchenObject food, out _))
                    return;
                float centredness = GestureRecognizers.SwipeCentredness(downPosition, position,
                    ScreenPoint(food.transform.position), Screen.height * FoodScreenRadius);
                if (centredness >= 0f)
                    Report(GestureFamily.Cut, centredness);
                return;
            case GestureFamily.Flip:
                if (flippedThisCook || tool.CurrentState != CookingTool.State.Cooking)
                    return;
                bool quick = Time.unscaledTime - downTime <= FlipMaxSeconds;
                bool rose = position.y - downPosition.y >= Screen.height * FlipMinRise;
                if (!quick || !rose)
                    return;
                flippedThisCook = true;
                Report(GestureFamily.Flip, tool.CookFraction);
                return;
            case GestureFamily.Pour:
                EndPour();
                return;
        }
    }

    private void BeginPour()
    {
        if (pouredThisCook || pouring)
            return;
        if (tool.CurrentState == CookingTool.State.Idle && tool.RequiresManualStart && tool.IsReadyToStart)
        {
            // Pressing down on a manual-start tool (Blender, ice-cream machine) is the start.
            Report(GestureFamily.Pour, CookingTool.PourStartSignal);
        }
        else if (tool.CurrentState != CookingTool.State.Cooking)
        {
            return;
        }
        pouring = true;
        pourStart = Time.time;
    }

    private void EndPour()
    {
        if (!pouring)
            return;
        pouring = false;
        pouredThisCook = true;
        Report(GestureFamily.Pour, CurrentPourLevel);
    }

    private float CurrentPourLevel => GestureRecognizers.PourLevel(Time.time - pourStart,
        GestureRecognizers.PourFillSeconds(tool.CookingTimeMax, tool.RequiresManualStart));

    private void UpdatePrompt()
    {
        if (prompt == null)
            return;
        CookingTool.State state = tool.CurrentState;
        switch (tool.GestureFamily)
        {
            case GestureFamily.Cut:
                if (tool.HasAnyFood && tool.IsCuttingActive)
                    prompt.Show("Swipe across the food to cut", true, tool.CookFraction, 0f, 0f);
                else
                    prompt.Hide();
                break;
            case GestureFamily.Flip:
                if (state == CookingTool.State.Cooking && !flippedThisCook)
                    prompt.Show("Flick up to flip in the green", true, tool.CookFraction,
                        GestureScoring.FlipWindowStart, GestureScoring.FlipWindowEnd);
                else
                    prompt.Hide();
                break;
            case GestureFamily.Stir:
                if (state == CookingTool.State.Cooking)
                {
                    bool due = tool.CurrentStirPromptIndex != lastStirPrompt;
                    prompt.Show(due ? "Stir! Draw circles on the pot" : "Good stir", true, tool.CookFraction, 0f, 0f);
                }
                else
                    prompt.Hide();
                break;
            case GestureFamily.Pour:
                if (pouring)
                    prompt.Show("Release in the green", true, CurrentPourLevel / 1.2f,
                        GestureScoring.PourBandMin / 1.2f, GestureScoring.PourBandMax / 1.2f);
                else if (!pouredThisCook && (state == CookingTool.State.Cooking ||
                         (state == CookingTool.State.Idle && tool.RequiresManualStart && tool.IsReadyToStart)))
                    prompt.Show("Hold on the machine to pour", false, 0f, 0f, 0f);
                else
                    prompt.Hide();
                break;
            case GestureFamily.Watch:
                if (state == CookingTool.State.Cooked)
                    prompt.Show("Drag it out in the gold zone", true, tool.BurnFraction,
                        GestureScoring.WatchZoneStart, GestureScoring.WatchZoneEnd);
                else
                    prompt.Hide();
                break;
            default:
                prompt.Hide();
                break;
        }
    }

    private static Vector2 ScreenPoint(Vector3 world)
    {
        Camera camera = Camera.main;
        return camera != null ? (Vector2)camera.WorldToScreenPoint(world) : Vector2.zero;
    }
}
