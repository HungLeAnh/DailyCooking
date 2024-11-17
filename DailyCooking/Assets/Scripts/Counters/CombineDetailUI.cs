using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CombineDetailUI : MonoBehaviour
{
    [SerializeField] private GameObject _container;
    [SerializeField] private GameObject _ingredientContainer;
    [SerializeField] private Image _foodIcon;
    [SerializeField] private Transform _iconTemplate;

    private List<GameObject> foodIngredientIconGOList = new List<GameObject>();
    private void Awake()
    {
        _iconTemplate.gameObject.SetActive(false);
        _container.SetActive(false);
    }

    public void InitUI(CombineRecipeSO combineRecipeSO)
    {
        _container.SetActive(true);

        foreach (var gameObject in foodIngredientIconGOList)
        {
            Destroy(gameObject);
        }
        foodIngredientIconGOList.Clear();

        _foodIcon.sprite = combineRecipeSO.output.Sprite;
        foreach (KitchenObjectSO kitchenObjectSO in combineRecipeSO.input)
        {
            Transform iconTransform = Instantiate(_iconTemplate, _ingredientContainer.transform);
            iconTransform.gameObject.SetActive(true);
            iconTransform.GetComponent<FoodIngredientIconUI>().SetKitchenObjectSO(kitchenObjectSO);
            foodIngredientIconGOList.Add(iconTransform.gameObject);        
        }
    }
}
