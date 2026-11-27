using TMPro;
using UnityEngine;

// "Away" floating above an avatar whose player dropped or went quiet. Built in code, no prefab change.
public class AwayLabel : MonoBehaviour
{
    private const string Text = "Away";
    private const float Height = 2.4f;
    private GameObject label;

    public void SetVisible(bool visible)
    {
        if (visible && label == null)
            label = Create();
        if (label != null)
            label.SetActive(visible);
    }

    private GameObject Create()
    {
        var go = new GameObject("AwayLabel");
        go.transform.SetParent(transform, false);
        go.transform.localPosition = Vector3.up * Height;
        var tmp = go.AddComponent<TextMeshPro>();
        tmp.text = Text;
        tmp.fontSize = 4f;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = new Color(1f, 0.85f, 0.3f);
        return go;
    }

    private void LateUpdate()
    {
        if (label == null || !label.activeSelf) return;
        Camera cam = Camera.main;
        if (cam != null)
            label.transform.rotation = Quaternion.LookRotation(label.transform.position - cam.transform.position);
    }
}
