using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class FoodCompleteVisual : MonoBehaviour
{
    [Serializable]
    public struct KitchenObjectSO_GameObject
    {
        public List<GameObject> GameObjectList;
        public KitchenObjectSO kitchenObjectSO;
    }
    [SerializeField] private FoodSO _foodSO;
    [SerializeField] private TablewareKitchenObject tablewareKitchenObject;
    [SerializeField] private List<KitchenObjectSO_GameObject> KitchenObjectSO_GameObjectList;
    private void Start()
    {
        tablewareKitchenObject.OnIngredientAdded += TablewareKitchenObject_OnIngredientAdded;
        tablewareKitchenObject.OnEaten += TablewareKitchenObject_OnEaten;
        foreach (KitchenObjectSO_GameObject kitchenObjectSOGameObject in KitchenObjectSO_GameObjectList)
        {
            foreach (var item in kitchenObjectSOGameObject.GameObjectList)
            {
                item.SetActive(false);
            } 
        }
    }
    private void OnDestroy()
    {
        tablewareKitchenObject.OnIngredientAdded -= TablewareKitchenObject_OnIngredientAdded;
        tablewareKitchenObject.OnEaten -= TablewareKitchenObject_OnEaten;
    }

    private void TablewareKitchenObject_OnEaten(object sender, EventArgs e)
    {
        foreach (KitchenObjectSO_GameObject kitchenObjectSOGameObject in KitchenObjectSO_GameObjectList)
        {
            foreach (var item in kitchenObjectSOGameObject.GameObjectList)
            {
                item.SetActive(false);
            }
        }
    }

    private void TablewareKitchenObject_OnIngredientAdded(object sender, TablewareKitchenObject.OnIngredientAddedEventArgs e)
    {

        foreach (KitchenObjectSO_GameObject kitchenObjectSOGameObject in KitchenObjectSO_GameObjectList)
        {
            if (kitchenObjectSOGameObject.kitchenObjectSO == e.KitchenObjectSO)
            {
                foreach (var item in kitchenObjectSOGameObject.GameObjectList)
                {
                    item.SetActive(true);
                }
            }
        }
    }
}
