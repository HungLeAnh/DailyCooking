using System;
using System.Collections.Generic;
using UnityEngine;

public class FoodCompleteVisual : MonoBehaviour
{
    [Serializable]
    public struct KitchenObjectSO_GameObject
    {
        public GameObject GameObject;
        public KitchenObjectSO kitchenObjectSO;
    }
    [SerializeField] private FoodSO _foodSO;
    [SerializeField] private TablewareKitchenObject tablewareKitchenObject;
    [SerializeField] private List<KitchenObjectSO_GameObject> KitchenObjectSO_GameObjectList;
    private void Start()
    {
        tablewareKitchenObject.OnIngredientAdded += TablewareKitchenObject_OnIngredientAdded;
        foreach (KitchenObjectSO_GameObject kitchenObjectSOGameObject in KitchenObjectSO_GameObjectList)
        {
            kitchenObjectSOGameObject.GameObject.SetActive(false);
        }
    }

    private void TablewareKitchenObject_OnIngredientAdded(object sender, TablewareKitchenObject.OnIngredientAddedEventArgs e)
    {
        if (e.foodSO != _foodSO)
            return;
        foreach (KitchenObjectSO_GameObject kitchenObjectSOGameObject in KitchenObjectSO_GameObjectList)
        {
            if (kitchenObjectSOGameObject.kitchenObjectSO == e.KitchenObjectSO)
            {
                kitchenObjectSOGameObject.GameObject.SetActive(true);
            }
        }
    }
}
