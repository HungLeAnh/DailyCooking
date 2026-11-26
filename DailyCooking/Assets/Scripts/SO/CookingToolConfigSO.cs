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

    [Header("Gesture (close-up)")]
    [Tooltip("The one gesture this tool uses in its close-up; decides how its cooking step is scored.")]
    public GestureFamily gestureFamily = GestureFamily.None;

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

    [Header("Commit / Presentation")]
    [Tooltip("True when the ingredients are swallowed on insertion: their meshes are hidden and " +
             "only the tool HUD shows what is inside (the Teapot).")]
    public bool hidesContents;
    [Tooltip("True when nothing can be taken back out of the tool once it went in.")]
    public bool commitsInputs;

    [Header("Output Vessel (optional)")]
    [Tooltip("Item the player must hold to collect the finished output. The vessel is consumed " +
             "and becomes the served result (e.g. the Teapot needs an Empty Cup for the tea).")]
    public KitchenObjectSO outputVessel;

    [Header("Boiling (Frying)")]
    [Tooltip("True when this frying tool may use recipes marked 'requiresBoilingTool' " +
             "(the Kettle). Pan and Grill leave it off.")]
    public bool canBoilWater;

    [Header("Allowed Ingredients (optional)")]
    [Tooltip("Leave empty to accept any ingredient the recipe database knows. Fill it to restrict " +
             "this tool to specific ingredients (e.g. Coffee Machine -> Coffee Bean only).")]
    public List<KitchenObjectSO> allowedIngredients = new List<KitchenObjectSO>();

    // Empty list means unrestricted, so existing tools (such as the Blender) keep their
    // current behaviour without any asset changes.
    public bool AllowsIngredient(KitchenObjectSO ingredient)
    {
        if (ingredient == null)
            return false;
        return allowedIngredients == null ||
               allowedIngredients.Count == 0 ||
               allowedIngredients.Contains(ingredient);
    }

    private void OnValidate()
    {
        if (supportsOptionMenu && (toolTypes == null || !toolTypes.Contains(CookingToolType.Combine)))
            UnityEngine.Debug.LogError($"{name}: option menu requires Combine in toolTypes.", this);
    }

}