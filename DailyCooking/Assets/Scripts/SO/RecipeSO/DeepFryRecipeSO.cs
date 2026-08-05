using UnityEngine;

[CreateAssetMenu()]
public class DeepFryRecipeSO : ScriptableObject
{
    public KitchenObjectSO input;
    public KitchenObjectSO output;
    public float deepFryTimerMax;
}
