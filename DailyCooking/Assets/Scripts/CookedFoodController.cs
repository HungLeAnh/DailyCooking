// CookedFoodController.cs
//
// Runtime visual component for the Cooked Food/CookedFoodLit shader.
// Drives the shader's "_CookAmount" property (0 = raw pink, 1 = fully seared/browned)
// via MaterialPropertyBlocks, so the source material assets are NEVER modified.
//
// Modes:
//  - Gameplay  : pulls cook progress from the parent CookingTool (stove/oven/fryer)
//                while it is cooking. Place a cookable food on a cooking counter and
//                it will sear in real time.
//  - AutoAnimate: oscillates 0..1 for preview.
//  - Manual    : uses the serialized `cook` value.
//
// Attach to a GameObject that has (or has a child with) a MeshRenderer using a CookedFoodLit material.
using UnityEngine;

[DisallowMultipleComponent]
public class CookedFoodController : MonoBehaviour
{
    [Header("Cooking")]
    [Range(0f, 1f)]
    [Tooltip("Cook progress: 0 = raw, 1 = fully seared/browned. Used when Gameplay Driven and Auto Animate are off.")]
    public float cook = 0f;

    [Header("Driving Mode")]
    [Tooltip("Pull _CookAmount from the cooking tool this food is placed on.")]
    public bool gameplayDriven = true;

    [Tooltip("When true, cook oscillates 0..1 automatically so you can see the transition.")]
    public bool autoAnimate = false;
    public float animateSpeed = 0.4f;

    private static readonly int CookAmountId = Shader.PropertyToID("_CookAmount");

    private MeshRenderer[] meshRenderers;
    private MaterialPropertyBlock[] propertyBlocks;
    private KitchenObject kitchenObject;
    private float animTimer;

    private void Awake()
    {
        CacheRenderers();
        kitchenObject = GetComponent<KitchenObject>();
    }

    private void CacheRenderers()
    {
        meshRenderers = GetComponentsInChildren<MeshRenderer>(true);
        propertyBlocks = new MaterialPropertyBlock[meshRenderers.Length];
        for (int i = 0; i < meshRenderers.Length; i++)
        {
            propertyBlocks[i] = new MaterialPropertyBlock();
        }
    }

    private void Update()
    {
        float cookValue;
        if (gameplayDriven)
        {
            cookValue = GetGameplayCook();
        }
        else if (autoAnimate)
        {
            animTimer += Time.deltaTime * animateSpeed;
            cookValue = Mathf.Sin(animTimer) * 0.5f + 0.5f;
        }
        else
        {
            cookValue = cook;
        }

        SetCook(cookValue);
    }

    private float GetGameplayCook()
    {
        if (kitchenObject == null) kitchenObject = GetComponent<KitchenObject>();
        if (kitchenObject == null) return 0f;

        IKitchenObjectParent parent = kitchenObject.GetKitchenObjectParent();
        CookingTool cookingTool = parent as CookingTool;
        if (cookingTool != null && cookingTool.CurrentState == CookingTool.State.Cooking)
        {
            return Mathf.Clamp01(cookingTool.GetProgress());
        }
        return 0f;
    }

    public void SetCook(float value)
    {
        if (meshRenderers == null || meshRenderers.Length == 0) CacheRenderers();
        if (meshRenderers == null || propertyBlocks == null) return;
        for (int i = 0; i < meshRenderers.Length; i++)
        {
            if (meshRenderers[i] == null || propertyBlocks[i] == null) continue;
            meshRenderers[i].GetPropertyBlock(propertyBlocks[i]);
            propertyBlocks[i].SetFloat(CookAmountId, value);
            meshRenderers[i].SetPropertyBlock(propertyBlocks[i]);
        }
    }

    private void OnValidate()
    {
        if (meshRenderers == null) CacheRenderers();
        SetCook(gameplayDriven ? GetGameplayCook() : cook);
    }
}
