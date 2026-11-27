using TMPro;
using UnityEngine;
using UnityEngine.UI;

// A small, non-blocking "Reconnecting…" banner on top of everything. Built in code so it needs no
// prefab and survives scene loads.
public class ReconnectingOverlay : MonoBehaviour
{
    private const string Text = "Reconnecting…";
    private static ReconnectingOverlay instance;
    private GameObject panel;

    public static void SetVisible(bool visible)
    {
        if (instance == null)
        {
            if (!visible) return;
            instance = Create();
        }
        instance.panel.SetActive(visible);
    }

    private static ReconnectingOverlay Create()
    {
        var root = new GameObject(nameof(ReconnectingOverlay));
        DontDestroyOnLoad(root);
        var canvas = root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 32000;
        var scaler = root.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        var panel = new GameObject("Panel", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(root.transform, false);
        var rt = (RectTransform)panel.transform;
        rt.anchorMin = new Vector2(0.5f, 1f);
        rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(0f, -40f);
        rt.sizeDelta = new Vector2(520f, 90f);
        var image = panel.GetComponent<Image>();
        image.color = new Color(0f, 0f, 0f, 0.7f);
        image.raycastTarget = false;

        var label = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        label.transform.SetParent(panel.transform, false);
        var lrt = (RectTransform)label.transform;
        lrt.anchorMin = Vector2.zero;
        lrt.anchorMax = Vector2.one;
        lrt.offsetMin = Vector2.zero;
        lrt.offsetMax = Vector2.zero;
        var tmp = label.GetComponent<TextMeshProUGUI>();
        tmp.text = Text;
        tmp.fontSize = 44;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        tmp.raycastTarget = false;

        var overlay = root.AddComponent<ReconnectingOverlay>();
        overlay.panel = panel;
        return overlay;
    }
}
