using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;

public class TablewareKitchenObject : KitchenObject, IInteractable
{
    public event EventHandler<OnIngredientAddedEventArgs> OnIngredientAdded;
    public class OnIngredientAddedEventArgs : EventArgs
    {
        public KitchenObjectSO KitchenObjectSO;
    }

    [SerializeField] private List<KitchenObjectSO> validKitchenObjectSOList;
    [SerializeField] private GameObject[] visualGameObjectArray;


    private List<KitchenObjectSO> _ingredientSOList;


    private void Awake()
    {
        _ingredientSOList = new List<KitchenObjectSO>();
    }

    public bool TryAddIngredient(KitchenObjectSO kitchenObjectSO)
    {

        //Debug.Log("Try add ingredient");
        if (!validKitchenObjectSOList.Contains(kitchenObjectSO))
        {
            //Debug.Log("add invalid object");
            return false;
        }
        if (_ingredientSOList.Contains(kitchenObjectSO))
        {
            //Already has this type
            //Debug.Log("Already has this type object");
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
    public void InteractEvent(PlayerStateMachine playerStateMachine)
    {
        Debug.LogError("Table: InteractEvent called");
        if (!playerStateMachine.HasKitchenObject())
        {
            // Player is not carrying anything

        }
        else
        {

        }
    }

    public void InteractAlternateEvent(PlayerStateMachine playerStateMachine)
    {

    }

    public void OnSelected()
    {
        Show();
    }

    public void OnDeselected()
    {
        Hide();
    }

    public void Show()
    {
        foreach (var visualGameObject in visualGameObjectArray)
        {
            visualGameObject.SetActive(true);
        }

    }
    public void Hide()
    {
        foreach (var visualGameObject in visualGameObjectArray)
        {
            visualGameObject.SetActive(false);
        }
    }
}
