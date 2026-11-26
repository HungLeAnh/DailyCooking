using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Close-up HUD prompt for the current gesture: a hint line and a bar with the scoring zone
// (flip window, pour band, watch golden zone). Never blocks raycasts.
public class GesturePromptUI : MonoBehaviour
{
    [SerializeField] private RectTransform bar;
    [SerializeField] private RectTransform fill;
    [SerializeField] private RectTransform zone;
    [SerializeField] private TextMeshProUGUI hint;

    public void Hide()
    {
        if (gameObject.activeSelf)
            gameObject.SetActive(false);
    }

    // value and the zone are 0-1 along the bar; showBar false hides the bar (hint only).
    public void Show(string text, bool showBar, float value, float zoneStart, float zoneEnd)
    {
        if (!gameObject.activeSelf)
            gameObject.SetActive(true);
        if (hint != null)
            hint.text = text;
        if (bar != null)
            bar.gameObject.SetActive(showBar);
        if (!showBar)
            return;
        if (fill != null)
        {
            fill.anchorMin = new Vector2(0f, 0f);
            fill.anchorMax = new Vector2(Mathf.Clamp01(value), 1f);
        }
        if (zone != null)
        {
            zone.anchorMin = new Vector2(Mathf.Clamp01(zoneStart), 0f);
            zone.anchorMax = new Vector2(Mathf.Clamp01(zoneEnd), 1f);
        }
    }
}
