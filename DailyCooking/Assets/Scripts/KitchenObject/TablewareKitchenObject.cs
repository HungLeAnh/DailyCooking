using System.Collections.Generic;
using System;
using UnityEngine;

public class TablewareKitchenObject : KitchenObject, IInteractable
{
    public event EventHandler<OnIngredientAddedEventArgs> OnIngredientAdded;
    public class OnIngredientAddedEventArgs : EventArgs
    {
        public KitchenObjectSO KitchenObjectSO;
    }
    public event EventHandler OnEaten;
    public event EventHandler OnServed;

    [SerializeField] private List<KitchenObjectSO> validKitchenObjectSOList;
    [SerializeField] private GameObject[] visualGameObjectArray;
    [SerializeField] private GameObject[] tablewareGameObjectArray;
    [SerializeField] private GameObject[] eatenGameObjectArray;

    private List<KitchenObjectSO> _ingredientSOList;
    private bool isEaten = false;

    private int cash;
    private int exp;

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
        if (!isEaten)
            return;

        if (!playerStateMachine.HasKitchenObject())
        {
            SetKitchenObjectParent(playerStateMachine);
            KitchenGameManager.Instance.CollectCash(cash,exp);
            cash = 0;   
            exp = 0;
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
    public void SetEaten(int cash, int exp)
    {
        this.cash = cash;
        this.exp = exp;
        isEaten = true;
        foreach (var visualGameObject in tablewareGameObjectArray)
        {
            visualGameObject.SetActive(false);
        }
        foreach (var eatenGameObject in eatenGameObjectArray)
        {
            eatenGameObject.SetActive(true);
        }
        OnEaten?.Invoke(this, EventArgs.Empty);

    }
    public void Serve()
    {
        OnServed?.Invoke(this, EventArgs.Empty);
    }
}
