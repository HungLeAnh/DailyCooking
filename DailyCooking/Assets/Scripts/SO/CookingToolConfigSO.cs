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

    [Header("Option Menu (Combine)")]
    [Tooltip("True when this tool uses the option-menu flow (Combine/Pot).")]
    public bool supportsOptionMenu;

    [Header("Appliance Visual")]
    [Tooltip("Model instantiated under CounterVisual for this appliance.")]
    public GameObject visualModel;
    public float visualScale = 1f;

    [Header("Layout")]
    [Tooltip("Vertical offset from the visual bounds top to the pan center.")]
    public float panTopOffsetY = -0.04f;
    [Tooltip("Vertical offset from the visual bounds top to CounterTopPos.")]
    public float counterTopOffsetY = 0.13f;
}