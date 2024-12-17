using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class DeliveryManager : MonoBehaviour
{

    public event EventHandler OnRecipeSpawned;
    public event EventHandler OnRecipeCompleted;
    public event EventHandler OnRecipeSuccess;
    public event EventHandler OnRecipeFailed;

    [SerializeField] private List<FoodSO> FoodSOList;

    public static DeliveryManager Instance { get; private set; }



    private List<FoodSO> waitingRecipeSOList;
    private float spawnRecipeTimer;
    private float spawnRecipeTimerMax = 4f;
    private int waitingRecipesMax = 4;
    private int successfulRecipesAmount;


    private void Awake()
    {
        Instance = this;
        waitingRecipeSOList = new List<FoodSO>();
    }
    private void Update()
    {
        spawnRecipeTimer -= Time.deltaTime;
        if (spawnRecipeTimer <= 0f)
        {
            spawnRecipeTimer = spawnRecipeTimerMax;

            if (KitchenGameManager.Instance.IsGamePlaying() && waitingRecipeSOList.Count < waitingRecipesMax)
            {
                FoodSO waitingRecipeSO = FoodSOList[UnityEngine.Random.Range(0, FoodSOList.Count)];

                waitingRecipeSOList.Add(waitingRecipeSO);
                OnRecipeSpawned?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    public void DeliverRecipe(TablewareKitchenObject tablewareKitchenObject)
    {
        foreach (var waitingFood in waitingRecipeSOList.ToList())
        {
            if(waitingFood.recipeName == tablewareKitchenObject.FoodSO.recipeName)
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
