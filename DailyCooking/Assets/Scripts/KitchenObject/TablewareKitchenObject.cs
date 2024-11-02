using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;

public class TablewareKitchenObject : KitchenObject
{
    public event EventHandler<OnIngredientAddedEventArgs> OnIngredientAdded;
    public class OnIngredientAddedEventArgs : EventArgs
    {
        public KitchenObjectSO KitchenObjectSO;
    }

    [SerializeField] private List<KitchenObjectSO> validKitchenObjectSOList;
    [SerializeField] private List<FoodSO> tablewareFoodSOList;

    private List<KitchenObjectSO> ingredientSOList;
    private List<FoodSO> possibleFoodList;
    private void Awake()
    {
        ingredientSOList = new List<KitchenObjectSO>();
        possibleFoodList = new List<FoodSO>(tablewareFoodSOList);
    }

    public bool TryAddIngredient(KitchenObjectSO kitchenObjectSO)
    {
        if (!validKitchenObjectSOList.Contains(kitchenObjectSO))
        {
            return false;
        }
        if (ingredientSOList.Contains(kitchenObjectSO))
        {
            //Already has this type
            return false;
        }
        else
        {
            if (ingredientSOList.Count == 0)
            {
                foreach(var foodSO in possibleFoodList.ToList())
                {
                    if (foodSO.kitchenObjectSOList[0] != kitchenObjectSO)
                        possibleFoodList.Remove(foodSO);
                }
            }
            else
            {
                foreach (var foodSO in possibleFoodList.ToList())
                {
                    if (foodSO.kitchenObjectSOList.Count <= ingredientSOList.Count||
                        foodSO.kitchenObjectSOList[ingredientSOList.Count] != kitchenObjectSO)
                        possibleFoodList.Remove(foodSO);
                }
            }

            if (possibleFoodList.Count > 0)
            {
                ingredientSOList.Add(kitchenObjectSO);
                OnIngredientAdded?.Invoke(this, new OnIngredientAddedEventArgs
                {
                    KitchenObjectSO = kitchenObjectSO,
                });
                return true;
            }
            else
                return false;

        }
    }
    public List<KitchenObjectSO> GetKitchenObjectSOList()
    {
        return ingredientSOList;
    }
    public List<FoodSO> GetPossibleFoodSOList()
    {
        return possibleFoodList;
    }
}
