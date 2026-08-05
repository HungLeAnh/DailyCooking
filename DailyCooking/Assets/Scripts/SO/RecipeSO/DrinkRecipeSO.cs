using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu()]
public class DrinkRecipeSO : ScriptableObject
{
    public List<KitchenObjectSO> input;
    public KitchenObjectSO output;
    public float drinkTimerMax;
}
