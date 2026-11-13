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

    public void Hide()
    {
        if (_container != null)
            _container.SetActive(false);
    }

    public void InitUI(CombineRecipeSO combineRecipeSO)
    {
        if (combineRecipeSO == null)
        {
            Hide();
            return;
        }
        _container.SetActive(true);
        ClearIcons();

        SetOutputIcon(combineRecipeSO.output);
        AddIcons(combineRecipeSO.input);
    }

    // HUD-only tools (the Teapot) show the items currently inside. No output icon until the
    // tool swaps its contents for the finished drink.
    public void InitUI(IReadOnlyList<KitchenObjectSO> inputs, KitchenObjectSO output)
    {
        if (inputs == null || inputs.Count == 0)
        {
            Hide();
            return;
        }
        _container.SetActive(true);
        ClearIcons();

        SetOutputIcon(output);
        AddIcons(inputs);
    }

    private void SetOutputIcon(KitchenObjectSO output)
    {
        if (_foodIcon == null)
            return;
        _foodIcon.gameObject.SetActive(output != null);
        if (output != null)
            _foodIcon.sprite = output.Sprite;
    }

    private void ClearIcons()
    {
        foreach (var gameObject in foodIngredientIconGOList)
        {
            Destroy(gameObject);
        }
        foodIngredientIconGOList.Clear();
    }

    private void AddIcons(IReadOnlyList<KitchenObjectSO> inputs)
    {
        if (inputs == null)
            return;
        foreach (KitchenObjectSO kitchenObjectSO in inputs)
        {
            if (kitchenObjectSO == null)
                continue;
            Transform iconTransform = Instantiate(_iconTemplate, _ingredientContainer.transform);
            iconTransform.gameObject.SetActive(true);
            iconTransform.GetComponent<FoodIngredientIconUI>().SetKitchenObjectSO(kitchenObjectSO);
            foodIngredientIconGOList.Add(iconTransform.gameObject);
        }
    }
}
