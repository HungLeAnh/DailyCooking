using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class DeliveryManager : SimpleSingleton<DeliveryManager>
{

    [SerializeField] private List<FoodSO> FoodSOList;

    private List<FoodSO> unlockFoodList;
    private List<KitchenObjectSO> unlockIngredient;
    private void Awake()
    {
        unlockFoodList = new List<FoodSO>();
        unlockIngredient = new List<KitchenObjectSO>();
    }

    public void OnDestroy()
    {
        unlockFoodList.Clear();
        unlockIngredient.Clear();
    }
    public void Init()
    {
        unlockFoodList.Clear();
        unlockFoodList.Clear();
        unlockIngredient.Clear();
        foreach (var counterController in CounterModules.Instance.BaseCounterControllers)
        {
            if(counterController == null)
                continue;
            AddUnlockIngredient(counterController);
        }

    }

    public void AddUnlockIngredient(BaseCounterController counterController)
    {
        IContainerCounter containerCounter = counterController as IContainerCounter;
        if(containerCounter == null) return;

        KitchenObjectSO kitchenObjectSO = containerCounter.GetContainerKitchenObjectType();
        if (kitchenObjectSO != null)
        {
            if(!unlockIngredient.Contains(kitchenObjectSO))
                unlockIngredient.Add(kitchenObjectSO);
        }        
        GetUnlockFood();

    }

    private void GetUnlockFood()
    {
        foreach(var foodSO in FoodSOList)
        {
            bool isUnlocked = true;
            foreach (var ingredient in foodSO.kitchenObjectSOList)
            {
                isUnlocked = isUnlocked && IsIngredientUnlocked(ingredient);
                if(!isUnlocked)
                {
                    break;
                }
            }
            if (isUnlocked)
            {
                if (!unlockFoodList.Contains(foodSO))
                    unlockFoodList.Add(foodSO);
            }
        }
    }

    private bool IsIngredientUnlocked(KitchenObjectSO ingredient)
    {
        foreach(var unlockIngredient in unlockIngredient)
        {
            if (unlockIngredient == ingredient)
            {
                return true;
            }
            else
            {
                var cuttingRecipeSO = KitchenGameManager.Instance.CuttingRecipeSOList.Find(x => x.input == unlockIngredient &&
                                                                       x.output == ingredient);
                var fryingRecipeSO = KitchenGameManager.Instance.FryingRecipeSOList.Find(x => x.input == unlockIngredient &&
                                                                       x.output == ingredient);
                if (cuttingRecipeSO != null || fryingRecipeSO != null)
                    return true;
            }

        }
        return false;
    }
    public FoodSO GetUnlockedFood()
    {
        return unlockFoodList[UnityEngine.Random.Range(0, unlockFoodList.Count)];
    }
}
