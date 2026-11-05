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
        Cutting,
    }

    [Header("Tool Types (recipe categories this tool can process)")]
    [Tooltip("Set multiple (e.g. DeepFry + Combine) for tools like the Pot.")]
    public List<CookingToolType> toolTypes = new List<CookingToolType>();

    public bool Supports(CookingToolType type)
    {
        return toolTypes != null && toolTypes.Contains(type);
    }

    [Header("Option Menu (Combine)")]
    [Tooltip("True when this tool uses the option-menu flow (Combine/Pot).")]
    public bool supportsOptionMenu;

    [Header("Burning")]
    [Tooltip("False for tools that should never burn their output, such as the Blender.")]
    public bool supportsBurning = true;

    [Header("Ingredients")]
    [Tooltip("How many ingredients the tool can hold at once. 1 keeps the classic single-slot behavior; " +
             "raise it for tools that take several ingredients (e.g. the Blender).")]
    [Min(1)] public int maxFoodSlots = 1;

    [Header("Start")]
    [Tooltip("True when the player must press the tool to start once the ingredient set matches " +
             "(the Blender). False keeps the classic auto-start on a valid recipe.")]
    public bool requiresManualStart;

    private void OnValidate()
    {
        if (supportsOptionMenu && (toolTypes == null || !toolTypes.Contains(CookingToolType.Combine)))
            UnityEngine.Debug.LogError($"{name}: option menu requires Combine in toolTypes.", this);
    }

}