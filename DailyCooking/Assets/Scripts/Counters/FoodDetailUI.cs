using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class FoodDetailUI : MonoBehaviour
{
    [SerializeField] private TablewareKitchenObject tablewareKitchenObject;
    [SerializeField] private GameObject _container;
    [SerializeField] private GameObject _ingredientContainer;
    [SerializeField] private Transform _iconTemplate;

    private List<GameObject> foodIngredientIconGOList = new List<GameObject>();
    private void Awake()
    {
        _iconTemplate.gameObject.SetActive(false);
        _container.SetActive(false);
    }
    private void Start()
    {
        tablewareKitchenObject.OnIngredientAdded += TablewareKitchenObject_OnIngredientAdded;
        tablewareKitchenObject.OnServed += TablewareKitchenObject_OnServed;

    }
    
    private void TablewareKitchenObject_OnServed(object sender, EventArgs e)
    {
        _container.SetActive(false);
    }

    private void TablewareKitchenObject_OnIngredientAdded(object sender, TablewareKitchenObject.OnIngredientAddedEventArgs e)
    {
        AddIngredientIcon(e.KitchenObjectSO);
    }
    private void OnDestroy()
    {
        tablewareKitchenObject.OnIngredientAdded -= TablewareKitchenObject_OnIngredientAdded;
        tablewareKitchenObject.OnServed -= TablewareKitchenObject_OnServed;

        foreach (var gameObject in foodIngredientIconGOList)
        {
            Destroy(gameObject);
        }
        foodIngredientIconGOList.Clear();
    }
    public void AddIngredientIcon(KitchenObjectSO kitchenObjectSO)
    {
        _container.SetActive(true);
        Transform iconTransform = Instantiate(_iconTemplate, _ingredientContainer.transform);
        iconTransform.gameObject.SetActive(true);
        iconTransform.GetComponent<FoodIngredientIconUI>().SetKitchenObjectSO(kitchenObjectSO);
        foodIngredientIconGOList.Add(iconTransform.gameObject);        
        
    }
}
