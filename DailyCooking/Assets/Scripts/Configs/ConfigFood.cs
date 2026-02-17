using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ConfigFoodSO", menuName = "Configs/ConfigFood")]
[Serializable]
public class ConfigFood : ScriptableObject
{
    [SerializeField] private List<FoodSO> foodItems = new List<FoodSO>();
    public List<FoodSO> FoodItems => foodItems;
}
