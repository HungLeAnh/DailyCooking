using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ConfigFoodSO", menuName = "Configs/ConfigFood")]
[Serializable]
public class ConfigFood : ScriptableObject
{
    [SerializeField] private List<FoodSO> foodItems = new List<FoodSO>();
    public List<FoodSO> FoodItems => foodItems;

    public Dictionary<string, FoodSO> FoodItemDic { get; private set; } = new Dictionary<string, FoodSO>();
    public void Initialize()
    {
        FoodItemDic.Clear();
        foreach (var food in foodItems)
        {
            FoodItemDic[food.Guid] = food;
        }
    }
}
