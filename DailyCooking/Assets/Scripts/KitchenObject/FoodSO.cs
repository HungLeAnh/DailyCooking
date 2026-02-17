using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu()]
public class FoodSO : SerializableScriptableObject
{
    public List<KitchenObjectSO> kitchenObjectSOList;
    public Sprite Sprite;
    public string recipeName;
    public long price;
    public int exp;
    public FoodType foodType;
}
