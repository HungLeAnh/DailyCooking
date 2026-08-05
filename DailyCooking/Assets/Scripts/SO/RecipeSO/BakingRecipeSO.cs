using UnityEngine;

[CreateAssetMenu()]
public class BakingRecipeSO : ScriptableObject
{
    public KitchenObjectSO input;
    public KitchenObjectSO output;
    public float bakingTimerMax;
}
