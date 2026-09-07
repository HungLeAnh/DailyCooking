using UnityEngine;

[CreateAssetMenu(fileName = "DefaultGridConfig", menuName = "Configs/DefaultGridConfig")]
public class DefaultGridConfigSO : ScriptableObject
{
    [Tooltip("Size of the square grid (mirrors GameDefine.GridSize)")]
    public int gridSize = 5;

    [TextArea(10, 20)]
    [Tooltip("Initial grid layout JSON. Extracted from GameDefine.GridArrayDataInit to avoid hardcoded 3k-char constant.")]
    public string gridArrayJson;

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (string.IsNullOrEmpty(gridArrayJson))
            Debug.LogWarning($"{name}: gridArrayJson is empty. Default grid will not initialize.", this);
    }
#endif

    public string GetGridJsonOrFallback(string fallbackJson)
    {
        return string.IsNullOrEmpty(gridArrayJson) ? fallbackJson : gridArrayJson;
    }
}
