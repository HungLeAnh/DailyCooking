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

    [Header("Tool Types (recipe categories this tool can process)")]
    [Tooltip("Set multiple (e.g. DeepFry + Combine) for tools like the Pot.")]
    public List<CookingToolType> toolTypes = new List<CookingToolType>();

    public List<CookingToolType> EffectiveToolTypes
    {
        get
        {
            if (toolTypes != null && toolTypes.Count > 0)
                return toolTypes;
            return new List<CookingToolType>();
        }
    }

    public bool Supports(CookingToolType type)
    {
        return toolTypes != null && toolTypes.Contains(type);
    }

    [Header("Option Menu (Combine)")]
    [Tooltip("True when this tool uses the option-menu flow (Combine/Pot).")]
    public bool supportsOptionMenu;

    private void OnValidate()
    {
        if (supportsOptionMenu && (toolTypes == null || !toolTypes.Contains(CookingToolType.Combine)))
            UnityEngine.Debug.LogError($"{name}: option menu requires Combine in toolTypes.", this);
    }

}