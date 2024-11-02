using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu()]
public class FoodSO : ScriptableObject
{
    public List<KitchenObjectSO> kitchenObjectSOList;
    public string recipeName;
}
