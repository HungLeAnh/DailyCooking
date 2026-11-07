using UnityEngine;
using UnityEngine.UI;

// World-space start prompt drawn above the tool, in the same style as CombineHUD
// (world canvas + LookAtCamera), so it sits on the machine instead of a fixed HUD corner.
// Only the local player standing next to a tool that waits for a manual start sees it.
public class CookingStartButtonWorldUI : MonoBehaviour
{
    [SerializeField] private Button startButton;
    [Tooltip("Child object holding the button art. Toggled instead of this object so Update keeps running.")]
    [SerializeField] private GameObject container;
    [Tooltip("Seconds between checks. The prompt does not need per-frame accuracy.")]
    [SerializeField] private float refreshInterval = 0.1f;
    [Tooltip("Ignores extra clicks right after pressing, before the tool state replicates.")]
    [SerializeField] private float pressCooldown = 0.3f;

    private CookingTool _tool;
    private CookingToolCounterController _controller;
    private float _nextRefresh;
    private float _cooldownUntil;

    private void Awake()
    {
        if (startButton != null)
            startButton.onClick.AddListener(OnClick);
        SetShown(false);
    }

    private void OnDestroy()
    {
        if (startButton != null)
            startButton.onClick.RemoveListener(OnClick);
    }

    private void Update()
    {
        if (Time.unscaledTime < _nextRefresh)
            return;
        _nextRefresh = Time.unscaledTime + Mathf.Max(0.02f, refreshInterval);
        SetShown(ShouldShow());
    }

    private bool ShouldShow()
    {
        if (Time.unscaledTime < _cooldownUntil)
            return false;
        if (_tool == null)
            _tool = GetComponentInParent<CookingTool>();
        if (_tool == null || !_tool.IsSpawned || !_tool.IsReadyToStart)
            return false;
        if (_controller == null)
            _controller = CookingToolCounterController.FindControllerForTool(_tool);
        if (_controller == null)
            return false;
        PlayerStateMachine player = PlayerStateMachine.LocalInstance;
        if (player == null || player.Context == null || player.Context.Highlightable == null)
            return false;
        // The player's own highlight list is the same radius the taps use, so the prompt only
        // appears once the machine is close enough to be used.
        return player.Context.Highlightable.Contains(_controller);
    }

    private void SetShown(bool shown)
    {
        if (container == null || container == gameObject)
            return;
        if (container.activeSelf != shown)
            container.SetActive(shown);
    }

    private void OnClick()
    {
        if (_tool == null || PlayerStateMachine.LocalInstance == null)
            return;
        if (_controller == null)
            _controller = CookingToolCounterController.FindControllerForTool(_tool);
        if (_controller == null)
            return;
        PlayerStateMachine.LocalInstance.RequestStartCooking(_controller);
        _cooldownUntil = Time.unscaledTime + Mathf.Max(0f, pressCooldown);
        SetShown(false);
    }
}
