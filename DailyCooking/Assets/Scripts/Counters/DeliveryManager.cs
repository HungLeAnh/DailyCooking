using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class DeliveryManager : SimpleSingleton<DeliveryManager>
{

    public event EventHandler OnRecipeSpawned;
    public event EventHandler OnRecipeCompleted;
    public event EventHandler OnRecipeSuccess;
    public event EventHandler OnRecipeFailed;

    [SerializeField] private List<FoodSO> FoodSOList;

    private List<FoodSO> waitingRecipeSOList;
    private float spawnRecipeTimer;
    private float spawnRecipeTimerMax = 4f;
    private int waitingRecipesMax = 4;
    private int successfulRecipesAmount;

    private List<FoodSO> unlockFoodList;
    private List<KitchenObjectSO> unlockIngredient;
    private void Awake()
    {
        waitingRecipeSOList = new List<FoodSO>();
        unlockFoodList = new List<FoodSO>();
        unlockIngredient = new List<KitchenObjectSO>();
    }
    public void Init()
    {
        foreach (var counterController in CounterModules.Instance.BaseCounterControllers)
        {
            AddUnlockIngredient(counterController);
        }

    }

    public void AddUnlockIngredient(BaseCounterController counterController)
    {
        KitchenObjectSO kitchenObjectSO = counterController.BaseCounterView.GetContainerKitchenObjectType();
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

    private void Update()
    {
        spawnRecipeTimer -= Time.deltaTime;
        if (spawnRecipeTimer <= 0f)
        {
            spawnRecipeTimer = spawnRecipeTimerMax;

            if (KitchenGameManager.Instance.IsGamePlaying() && waitingRecipeSOList.Count < waitingRecipesMax)
            {
                FoodSO waitingRecipeSO = unlockFoodList[UnityEngine.Random.Range(0, unlockFoodList.Count)];

                waitingRecipeSOList.Add(waitingRecipeSO);
                OnRecipeSpawned?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    public void DeliverRecipe(TablewareKitchenObject tablewareKitchenObject)
    {
        foreach (var waitingFood in waitingRecipeSOList.ToList())
        {

                if (waitingFood.kitchenObjectSOList.Count == tablewareKitchenObject.GetKitchenObjectSOList().Count)
                {
                    //Has the same number of ingredients
                    bool plateContentMathesRecipe = true;

                    foreach (KitchenObjectSO recipeKitchenObjectSO in waitingFood.kitchenObjectSOList)
                    {
                        //Cycling through all ingredients in recipe
                        bool ingredientFound = false;
                        foreach (KitchenObjectSO plateKitchenObjectSO in tablewareKitchenObject.GetKitchenObjectSOList())
                        {
                            //Cycling through all ingredients in recipe
                            if (plateKitchenObjectSO == recipeKitchenObjectSO)
                            {
                                ingredientFound = true;
                                break;
                            }
                        }
                        if (!ingredientFound)
                        {
                            // This Recipe ingredient was not found on the plate
                            plateContentMathesRecipe = false;

                        }
                    }

                    if (plateContentMathesRecipe)
                    {
                        // Player delivered correct recipe 
                        waitingRecipeSOList.Remove(waitingFood);
                        successfulRecipesAmount++;
                        OnRecipeCompleted?.Invoke(this, EventArgs.Empty);
                        OnRecipeSuccess?.Invoke(this, EventArgs.Empty);
                        KitchenGameManager.Instance.ServeFood(waitingFood);
                        return;
                    }
                }
            
        }

        //No matches found
        //Player did not deliver correct recipe
        OnRecipeFailed?.Invoke(this, EventArgs.Empty);
    }

    public List<FoodSO> GetWaitingRecipeSOList()
    {
        return waitingRecipeSOList;
    }
    public int GetSuccessfulRecipeAmount()
    {
        return successfulRecipesAmount;
    }
}
