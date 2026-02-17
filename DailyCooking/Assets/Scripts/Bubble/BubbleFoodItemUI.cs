using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BubbleFoodItemUI : MonoBehaviour
{
    [SerializeField] private Image imageDish;
    [SerializeField] private GameObject _container;
    [SerializeField] private GameObject _ingredientContainer;
    [SerializeField] private Transform _iconTemplate;

    private List<GameObject> foodIngredientIconGOList = new List<GameObject>();
    private FoodSO foodSO;
    public void SetFood(FoodSO foodSO)
    {
        this.foodSO = foodSO;
        imageDish.sprite = foodSO.Sprite;        
        _container.SetActive(true);

        foreach (var kitchenObjectSO in foodSO.kitchenObjectSOList)
        {
            AddIngredientIcon(kitchenObjectSO);
        }
    }
    private void Awake()
    {
        _iconTemplate.gameObject.SetActive(false);
    }
    private void OnDisable()
    {
        foreach (var gameObject in foodIngredientIconGOList)
        {
            Destroy(gameObject);
        }
        foodIngredientIconGOList.Clear();
    }
    public void AddIngredientIcon(KitchenObjectSO kitchenObjectSO)
    {
        Transform iconTransform = Instantiate(_iconTemplate, _ingredientContainer.transform);
        iconTransform.gameObject.SetActive(true);
        iconTransform.GetComponent<Image>().sprite = kitchenObjectSO.Sprite;
        foodIngredientIconGOList.Add(iconTransform.gameObject);

    }
}