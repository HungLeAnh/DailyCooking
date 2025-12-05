using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class UIInventoryItem : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _itemCount = default;
    [SerializeField] private Image _itemPreviewImage = default;
    [SerializeField] private Image _bgImage = default;
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
            _itemButton.gameObject.SetActive(true);

            _itemPreviewImage.sprite = placedObject.icon;

            _itemCount.text = itemStack.Amount.ToString();
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
       
        if (ItemSelected != null)
        {
            ItemSelected.Invoke(placedObjectTypeSO);        
            //Debug.LogError("SelectItem:  "+placedObjectTypeSO.name);

        }
        else
        {
        }
    }

    public void UnselectItem()
    {
        _isSelected = false;
    }
}
