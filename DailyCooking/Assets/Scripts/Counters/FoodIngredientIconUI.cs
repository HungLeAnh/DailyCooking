using UnityEngine;
using UnityEngine.UI;

public class FoodIngredientIconUI : MonoBehaviour
{
    [SerializeField] private Image image;
    private KitchenObjectSO _kitchenSO;

    public void SetKitchenObjectSO(KitchenObjectSO kitchenObjectSO)
    {
        _kitchenSO = kitchenObjectSO;
        image.sprite = kitchenObjectSO.Sprite;
    }
}