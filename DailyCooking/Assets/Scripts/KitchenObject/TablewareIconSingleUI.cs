using UnityEngine;
using UnityEngine.UI;

public class TablewareIconSingleUI : MonoBehaviour
{
    [SerializeField] private Image image;

    public void SetKitchenObjectSO(KitchenObjectSO kitchenObjectSO)
    {
        image.sprite = kitchenObjectSO.Sprite;
    }
}
