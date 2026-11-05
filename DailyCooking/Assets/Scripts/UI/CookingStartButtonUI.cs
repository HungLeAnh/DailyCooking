using UnityEngine;
using UnityEngine.UI;

// Context HUD button: shown while the local player stands next to a tool whose ingredient set
// already matches a recipe and that waits for a manual start (the Blender).
// The component itself sits on an always-active object and only toggles `visualRoot`,
// otherwise Update would stop running after the first hide.
public class CookingStartButtonUI : MonoBehaviour
{
    [SerializeField] private Button startButton;
    [Tooltip("Child object with the button art. Must not be this GameObject itself.")]
    [SerializeField] private GameObject visualRoot;

    [Tooltip("Seconds between proximity checks. The HUD does not need per-frame accuracy.")]
    [SerializeField] private float refreshInterval = 0.1f;
    [Tooltip("Only tools closer than this (in meters) may show the button.")]
    [SerializeField] private float maxDistance = 6f;

    private CookingToolCounterController _target;
    private float _nextRefresh;

    private void Awake()
    {
        if (startButton != null)
            startButton.onClick.AddListener(OnClick);
        ApplyVisibility(false);
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

        _target = ShouldHide() ? null : FindReadyTool();
        ApplyVisibility(_target != null);
    }

    private bool ShouldHide()
    {
        if (PlayerStateMachine.LocalInstance == null)
            return true;
        if (UIPopupManager.Instance != null && UIPopupManager.Instance.IsShowingPopup())
            return true;
        GridBuildingSystem grid = GridBuildingSystem.Instance;
        if (grid != null && grid.BuildingPlacementManager != null && grid.BuildingPlacementManager.IsBuilding)
            return true;
        return false;
    }

    // Nearest nearby tool that is waiting for its start press.
    private CookingToolCounterController FindReadyTool()
    {
        PlayerStateMachine player = PlayerStateMachine.LocalInstance;
        if (player == null || player.Context == null || player.Context.Highlightable == null)
            return null;
        Vector3 position = player.transform.position;

        CookingToolCounterController best = null;
        float bestDistance = maxDistance;

        foreach (IHighlightable highlightable in player.Context.Highlightable)
        {
            if (!(highlightable is CookingToolCounterController tool) || !tool.CanPressStart())
                continue;
            float distance = Vector3.Distance(position, tool.transform.position);
            if (distance > bestDistance)
                continue;
            bestDistance = distance;
            best = tool;
        }
        return best;
    }

    private void ApplyVisibility(bool visible)
    {
        if (visualRoot == null || visualRoot == gameObject)
            return;
        if (visualRoot.activeSelf != visible)
            visualRoot.SetActive(visible);
    }

    private void OnClick()
    {
        if (_target == null || PlayerStateMachine.LocalInstance == null)
            return;
        PlayerStateMachine.LocalInstance.RequestStartCooking(_target);
        // The server flips the tool to Cooking; hide right away so double taps do nothing.
        _target = null;
        ApplyVisibility(false);
    }
}
