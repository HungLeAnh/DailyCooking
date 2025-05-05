using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
public enum InventoryTabType
{
    Customization,
    CookingItem,
    Recipe,
    Counter,
}
public enum ItemInventoryType
{
    Recipe,
    Utensil,
    Ingredient,
    Customisation,
    Dish,
}

public class UIInventoryPopup : UIPopup
{
    [SerializeField] private UIInventoryItem _itemPrefab = default;
    [SerializeField] private GameObject _contentParent = default;
    [SerializeField] private List<InventoryTab> _tabTypesList = new List<InventoryTab>();
    [SerializeField] private UIInventoryTabs _tabsPanel = default;

    private InventoryTab _selectedTab = default;
    private int selectedItemId = -1;
    private List<UIInventoryItem> _listItem = new List<UIInventoryItem>();
    private void Start()
    {
        foreach (var prefabSO in GridBuildingSystem.Instance.PlacedObjectDatabase.PlacedObjects)
        {
            var item = Instantiate(_itemPrefab, _contentParent.transform);
            item.ItemSelected += InspectItem;
            item.SetItem(prefabSO,false);
            item.gameObject.SetActive(true);
            _listItem.Add(item);
        }
    }
    private void OnEnable()
    {

        //_tabsPanel.TabChanged += OnChangeTab;

        for (int i = 0; i < _listItem.Count; i++)
        {
            _listItem[i].ItemSelected += InspectItem;
        }
        //sub switch tab here

    }

    private void OnDisable()
    {
        //_tabsPanel.TabChanged -= OnChangeTab;

        for (int i = 0; i < _listItem.Count; i++)
        {
            _listItem[i].ItemSelected -= InspectItem;
        }

        //unsub switch tab here
    }

    //private void OnSwitchTab(float orientation)
    //{
    //    Debug.Log("OnSwitchTab");
    //    if (orientation != 0)
    //    {
    //        bool isLeft = orientation < 0;
    //        int initialIndex = _tabTypesList.FindIndex(o => o == _selectedTab);
    //        if (initialIndex != -1)
    //        {
    //            if (isLeft)
    //            {
    //                initialIndex--;
    //            }
    //            else
    //            {
    //                initialIndex++;
    //            }

    //            initialIndex = Mathf.Clamp(initialIndex, 0, _tabTypesList.Count - 1);
    //        }

    //        OnChangeTab(_tabTypesList[initialIndex]);
    //    }
    //}

    //public void FillInventory(InventoryTabType _selectedTabType = InventoryTabType.CookingItem, bool isNearPot = false)
    //{

    //    if ((_tabTypesList.Exists(o => o.TabType == _selectedTabType)))
    //    {
    //        _selectedTab = _tabTypesList.Find(o => o.TabType == _selectedTabType);
    //    }
    //    else
    //    {
    //        if (_tabTypesList != null)
    //        {
    //            if (_tabTypesList.Count > 0)
    //            {
    //                _selectedTab = _tabTypesList[0];
    //            }
    //        }
    //    }

    //    if (_selectedTab != null)
    //    {
    //        SetTabs(_tabTypesList, _selectedTab);
    //        List<ItemStack> listItemsToShow = new List<ItemStack>();
    //        listItemsToShow = _currentInventory.Items.FindAll(o => o.Item.ItemType.TabType == _selectedTab);

    //        FillInvetoryItems(listItemsToShow);
    //    }
    //    else
    //    {
    //        Debug.LogError("There's no selected tab");
    //    }
    //}

    void SetTabs(List<InventoryTab> typesList, InventoryTab selectedType)
    {
        _tabsPanel.SetTabs(typesList, selectedType);
    }

    //void FillInvetoryItems(List<ItemStack> listItemsToShow)
    //{
    //    if (_listItem == null)
    //        _listItem = new List<UIInventoryItem>();

    //    int maxCount = Mathf.Max(listItemsToShow.Count, _listItem.Count);

    //    for (int i = 0; i < maxCount; i++)
    //    {
    //        if (i < listItemsToShow.Count)
    //        {
    //            bool isSelected = selectedItemId == i;
    //            _listItem[i].SetItem(listItemsToShow[i], isSelected);

    //        }
    //        else if (i < _listItem.Count)
    //        {
    //            _listItem[i].SetInactiveItem();
    //        }

    //    }


    //    if (selectedItemId >= 0)
    //    {
    //        UnselectItem(selectedItemId);
    //        selectedItemId = -1;
    //    }
    //    if (_listItem.Count > 0)
    //    {
    //        _listItem[0].SelectFirstElement();
    //    }
    //}

    //void UpdateItemInInventory(ItemStack itemToUpdate, bool removeItem)
    //{
    //    if (_listItem == null)
    //        _listItem = new List<UIInventoryItem>();

    //    if (removeItem)
    //    {
    //        if (_listItem.Exists(o => o.currentItem == itemToUpdate))
    //        {

    //            int index = _listItem.FindIndex(o => o.currentItem == itemToUpdate);
    //            _listItem[index].SetInactiveItem();
    //        }
    //    }
    //    else
    //    {
    //        int index = 0;

    //        //if the item has already been created
    //        if (_listItem.Exists(o => o.currentItem == itemToUpdate))
    //        {
    //            index = _listItem.FindIndex(o => o.currentItem == itemToUpdate);
    //        }
    //        //if the item needs to be created
    //        else
    //        {
    //            //if the new item needs to be instantiated
    //            if (_currentInventory.Items.Count > _listItem.Count)
    //            {
    //                UIInventoryItem instantiatedPrefab = Instantiate(_itemPrefab, _contentParent.transform) as UIInventoryItem;
    //                _listItem.Add(instantiatedPrefab);
    //            }

    //            //find the last instantiated game object not used
    //            index = _currentInventory.Items.Count;
    //        }

    //        bool isSelected = selectedItemId == index;
    //        _listItem[index].SetItem(itemToUpdate, isSelected);
    //    }
    //}

    public void InspectItem(PlacedObjectTypeSO itemToInspect)
    {
        if (_listItem.Exists(o => o.PlacedObjectTypeSO == itemToInspect))
        {
            int itemIndex = _listItem.FindIndex(o => o.PlacedObjectTypeSO == itemToInspect);

            //unselect selected Item
            if (selectedItemId >= 0 && selectedItemId != itemIndex)
                UnselectItem(selectedItemId);

            //change Selected ID 
            selectedItemId = itemIndex;

            //show Information

            //check if interactable

        }
        GridBuildingSystem.Instance.SetPlacedObjectTypeSO(_listItem[selectedItemId].PlacedObjectTypeSO);
    }

    void UnselectItem(int itemIndex)
    {
        if (_listItem.Count > itemIndex)
        {
            _listItem[itemIndex].UnselectItem();
        }
    }

    //void UpdateInventory()
    //{
    //    FillInventory(_selectedTab.TabType);
    //}

    //void OnChangeTab(InventoryTabSO tabType)
    //{
    //    FillInventory(tabType.TabType);
    //}

    public void SaveGrid()
    {
        GridBuildingSystem.Instance.SaveGrid();
    }
}
