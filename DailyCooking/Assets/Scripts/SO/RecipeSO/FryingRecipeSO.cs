using UnityEngine;

[CreateAssetMenu()]
public class FryingRecipeSO : ScriptableObject
{
    public KitchenObjectSO input;
    public KitchenObjectSO output;
    public float fryingTimerMax;
    [Tooltip("Only frying tools with canBoilWater (the Kettle) may run this recipe. " +
             "Used to keep Pan and Grill from boiling water.")]
    public bool requiresBoilingTool;

}