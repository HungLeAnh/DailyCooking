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


    private List<KitchenObjectSO> _ingredientSOList;

    public List<FoodSO> TablewareFoodSOList { get => tablewareFoodSOList; set => tablewareFoodSOList = value; }

    private void Awake()
    {
        _ingredientSOList = new List<KitchenObjectSO>();
    }

    public bool TryAddIngredient(KitchenObjectSO kitchenObjectSO)
    {

        //Debug.Log("Try add ingredient");
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
            _ingredientSOList.Add(kitchenObjectSO);
            OnIngredientAdded?.Invoke(this, new OnIngredientAddedEventArgs
            {
                KitchenObjectSO = kitchenObjectSO,
                
            });
            return true;
        }
    }
    public List<KitchenObjectSO> GetKitchenObjectSOList()
    {
        return _ingredientSOList;
    }

    
}
