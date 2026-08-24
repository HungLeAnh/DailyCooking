using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "CookingToolConfig", menuName = "SO/CookingToolConfig")]
public class CookingToolConfigSO : ScriptableObject
{
    public enum CookingToolType
    {
        Frying,
        Baking,
        DeepFry,
        Beverage,
        Combine,
    }

    [Header("Strategy")]
    public CookingToolType toolType;

    [Header("Tool Types (recipe categories this tool can process)")]
    [Tooltip("When empty, falls back to a single 'toolType'. Set multiple (e.g. DeepFry + Combine) for tools like the Pot.")]
    public List<CookingToolType> toolTypes = new List<CookingToolType>();

    public List<CookingToolType> EffectiveToolTypes
    {
        get
        {
            if (toolTypes != null && toolTypes.Count > 0)
                return toolTypes;
            return new List<CookingToolType> { toolType };
        }
    }

    public bool Supports(CookingToolType type)
    {
        return EffectiveToolTypes.Contains(type);
    }

    [Header("Visual")]
    [Tooltip("Optional cosmetic mesh spawned on this tool at runtime (pan/pot/etc.). Not networked.")]
    public GameObject visualPrefab;
    public float visualScale = 1f;

    [Header("Option Menu (Combine)")]
    [Tooltip("True when this tool uses the option-menu flow (Combine/Pot).")]
    public bool supportsOptionMenu;

}