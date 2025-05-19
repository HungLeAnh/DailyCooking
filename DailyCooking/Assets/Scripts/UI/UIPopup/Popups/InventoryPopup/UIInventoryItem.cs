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

    public void SetItem(ItemStack itemStack, bool isSelected)
    {
        if (itemStack == null) return;
        if (GridBuildingSystem.Instance.PlaceObjectTypeSODictionary.TryGetValue(itemStack.Item.PlacedObjectTypeSOGuid, out PlacedObjectTypeSO placedObject))
        {
            placedObjectTypeSO = placedObject;
            currentItem = itemStack;
            _isSelected = isSelected;

            gameObject.SetActive(true);
            _itemPreviewImage.gameObject.SetActive(true);
            _itemCount.gameObject.SetActive(true);
            //_bgImage.gameObject.SetActive(true);
            _imgSelected.gameObject.SetActive(true);
            _itemButton.gameObject.SetActive(true);
            _bgInactiveImage.gameObject.SetActive(false);

            _imgSelected.gameObject.SetActive(isSelected);

            _itemPreviewImage.sprite = placedObject.icon;

            _itemCount.text = itemStack.Amount.ToString();
            //_bgImage.color = itemStack.Item.ItemType.TypeColor;
        }
        else
        {
            gameObject.SetActive(false);
        }
    }

    public void SetInactiveItem()
    {
        currentItem = null;
        gameObject.SetActive(false);
        _isSelected = false;
        //_itemPreviewImage.gameObject.SetActive(false);
        //_itemCount.gameObject.SetActive(false);
        //_bgImage.gameObject.SetActive(false);
        //_imgSelected.gameObject.SetActive(false);
        //_itemButton.gameObject.SetActive(false);
        //_bgInactiveImage.gameObject.SetActive(true);
    }

    private void OnEnable()
    {
        if (_isSelected)
        { SelectItem(); }
    }
    public void OnItemClick()
    {
        _isSelected = !_isSelected;
        if (_isSelected)
        { 
            SelectItem(); 
        }
        else
        {
            UnselectItem();
        }
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
