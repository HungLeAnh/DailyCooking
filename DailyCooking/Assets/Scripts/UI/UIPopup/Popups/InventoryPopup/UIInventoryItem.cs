using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class UIInventoryItem : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _itemCount = default;
    [SerializeField] private Image _itemPreviewImage = default;
    [SerializeField] private Image _bgImage = default;
    [SerializeField] private Image _imgSelected = default;
    [SerializeField] private Image _bgInactiveImage = default;
    [SerializeField] public Button _itemButton = default;

    public UnityAction<PlacedObjectTypeSO> ItemSelected;

    [HideInInspector] public ItemStack currentItem;
    private PlacedObjectTypeSO placedObjectTypeSO;

    bool _isSelected = false;
    public PlacedObjectTypeSO PlacedObjectTypeSO { get => placedObjectTypeSO; set => placedObjectTypeSO = value; }

    public void SetItem(PlacedObjectTypeSO placedObject, bool isSelected)
    {
        _isSelected = isSelected;
        _itemPreviewImage.gameObject.SetActive(true);
        _itemCount.gameObject.SetActive(true);
        _bgImage.gameObject.SetActive(true);
        _imgSelected.gameObject.SetActive(true);
        _itemButton.gameObject.SetActive(true);
        _bgInactiveImage.gameObject.SetActive(false);

        PlacedObjectTypeSO = placedObject;

        _imgSelected.gameObject.SetActive(isSelected);

        _itemPreviewImage.sprite = placedObject.icon;


    }
    public void SetItem(ItemStack itemStack, bool isSelected)
    {
        _isSelected = isSelected;
        _itemPreviewImage.gameObject.SetActive(true);
        _itemCount.gameObject.SetActive(true);
        _bgImage.gameObject.SetActive(true);
        _imgSelected.gameObject.SetActive(true);
        _itemButton.gameObject.SetActive(true);
        _bgInactiveImage.gameObject.SetActive(false);

        currentItem = itemStack;

        _imgSelected.gameObject.SetActive(isSelected);

        _itemPreviewImage.sprite = itemStack.Item.PreviewImage;

        _itemCount.text = itemStack.Amount.ToString();
        _bgImage.color = itemStack.Item.ItemType.TypeColor;
    }

    public void SetInactiveItem()
    {
        currentItem = null;
        _itemPreviewImage.gameObject.SetActive(false);
        _itemCount.gameObject.SetActive(false);
        _bgImage.gameObject.SetActive(false);
        _imgSelected.gameObject.SetActive(false);
        _itemButton.gameObject.SetActive(false);
        _bgInactiveImage.gameObject.SetActive(true);
    }

    public void SelectFirstElement()
    {
        _isSelected = true;
        _itemButton.Select();
        SelectItem();
    }

    private void OnEnable()
    {
        if (_isSelected)
        { SelectItem(); }
    }

   
    public void SelectItem()
    {
        _isSelected = true;
        //if (ItemSelected != null && currentItem != null && currentItem.Item != null)
        //{
        //    _imgSelected.gameObject.SetActive(true);
        //    ItemSelected.Invoke(placedObjectTypeSO);        
        //    Debug.LogError("SelectItem:  "+placedObjectTypeSO.name);

        //}        
        if (ItemSelected != null)
        {
            _imgSelected.gameObject.SetActive(true);
            ItemSelected.Invoke(placedObjectTypeSO);        
            //Debug.LogError("SelectItem:  "+placedObjectTypeSO.name);

        }
        else
        {
            _imgSelected.gameObject.SetActive(false);
        }
    }

    public void UnselectItem()
    {
        _isSelected = false;
        _imgSelected.gameObject.SetActive(false);
    }
}
