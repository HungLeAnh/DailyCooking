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
        public FoodSO foodSO;
        public KitchenObjectSO KitchenObjectSO;
    }

    [SerializeField] private List<KitchenObjectSO> validKitchenObjectSOList;
    [SerializeField] private List<FoodSO> tablewareFoodSOList;
    [SerializeField] private FoodDetailUI foodDetailUI;


    private List<KitchenObjectSO> _ingredientSOList;
    private FoodSO _foodSO;

    public FoodSO FoodSO => _foodSO;
    public List<FoodSO> TablewareFoodSOList { get => tablewareFoodSOList; set => tablewareFoodSOList = value; }

    private void Awake()
    {
        _ingredientSOList = new List<KitchenObjectSO>();
    }

    public bool TryAddIngredient(KitchenObjectSO kitchenObjectSO)
    {
        if (_foodSO == null) 
            return false;

        Debug.Log("Try add ingredient");
        if (!validKitchenObjectSOList.Contains(kitchenObjectSO))
        {
            Debug.Log("add invalid object");
            return false;
        }
        if (_ingredientSOList.Contains(kitchenObjectSO))
        {
            //Already has this type
            Debug.Log("Already has this type object");
            return false;
        }
        else
        { 
            if(!_foodSO.kitchenObjectSOList.Contains(kitchenObjectSO))
                return false;

            _ingredientSOList.Add(kitchenObjectSO);
            OnIngredientAdded?.Invoke(this, new OnIngredientAddedEventArgs
            {
                KitchenObjectSO = kitchenObjectSO,
                foodSO = _foodSO
            });
            return true;
        }
    }
    public List<KitchenObjectSO> GetKitchenObjectSOList()
    {
        return _ingredientSOList;
    }

    public void SetFoodSO(FoodSO foodSO)
    {
        _foodSO = foodSO;
        foodDetailUI.InitUI(_foodSO);
    }
}
