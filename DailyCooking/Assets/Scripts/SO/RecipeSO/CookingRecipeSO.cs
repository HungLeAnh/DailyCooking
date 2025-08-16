using UnityEngine;

[CreateAssetMenu(fileName = "CookingRecipe", menuName = "SO/CookingRecipe")]
public class CookingRecipeSO : ScriptableObject
{
    public KitchenObjectSO input;
    public KitchenObjectSO output;
    public float cookingTimerMax;
}