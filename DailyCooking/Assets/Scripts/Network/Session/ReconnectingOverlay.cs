using TMPro;
using UnityEngine;
using UnityEngine.UI;

// "Reconnecting…" on top of everything, in two forms: a small non-blocking banner (no word from the
// host for 5 s, short notices) and a full-screen, input-blocking panel while the client rejoins.
// Built in code so it needs no prefab and survives scene loads.
public class ReconnectingOverlay : MonoBehaviour
{
    private const string Text = "Reconnecting…";
    private static readonly Vector2 BannerSize = new Vector2(520f, 90f);
    private static ReconnectingOverlay instance;
    private GameObject panel;
    private Image background;
    private TextMeshProUGUI label;

    public static void SetVisible(bool visible) => SetVisible(visible, Text);

    // Banner form; also used for other short notices ("The host is watching an ad…").
    public static void SetVisible(bool visible, string text)
    {
        if (instance == null)
        {
            if (!visible) return;
            instance = Create();
        }
        instance.SetFullScreen(false);
        if (visible) instance.label.text = text;
        instance.panel.SetActive(visible);
    }

    // Full-screen form: covers and blocks the (emptied) scene while the client rejoins.
    public static void ShowBlocking(string text = Text)
    {
        if (instance == null) instance = Create();
        instance.SetFullScreen(true);
        instance.label.text = text;
        instance.panel.SetActive(true);
    }

    private void SetFullScreen(bool fullScreen)
    {
        var rt = (RectTransform)panel.transform;
        if (fullScreen)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = Vector2.zero;
            background.color = new Color(0.05f, 0.08f, 0.15f, 1f);
            background.raycastTarget = true;
        }
        else
        {
            rt.anchorMin = new Vector2(0.5f, 1f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0f, -40f);
            rt.sizeDelta = BannerSize;
            background.color = new Color(0f, 0f, 0f, 0.7f);
            background.raycastTarget = false;
        }
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
        root.AddComponent<GraphicRaycaster>();

        var panel = new GameObject("Panel", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(root.transform, false);

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
        overlay.background = panel.GetComponent<Image>();
        overlay.label = tmp;
        overlay.SetFullScreen(false);
        return overlay;
    }
}
