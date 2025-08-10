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

public class UIInventoryPopup : UIPopup
{
    public class Param
    {
        public bool isPlacingObject;
    }

    [SerializeField] private UIInventoryItem _itemPrefab = default;
    [SerializeField] private GameObject _contentParent = default;
    [SerializeField] private InventoryTabDatabase inventoryTabDatabase = default;
    [SerializeField] private UIInventoryTabs _tabsPanel = default;

    private InventoryTab _selectedTab;
    private int selectedItemId = -1;
    private List<UIInventoryItem> _listItem = new List<UIInventoryItem>();
    private bool isPlacingObject = false;
    public override void SetupPopup()
    {
        base.SetupPopup();

        //foreach (var prefabSO in GridBuildingSystem.Instance.PlacedObjectDatabase.PlacedObjects)
        foreach (var prefabSO in GameManager.Instance.GameData.InventoryData.Items)
        {
            CreateInventoryItem(prefabSO);
        }
        _tabsPanel.Setup(inventoryTabDatabase.TabTypesList);
        _tabsPanel.SetTabs(inventoryTabDatabase.TabTypesList[0]);
        _selectedTab = inventoryTabDatabase.TabTypesList[0];
        FillInventory(_selectedTab.TabType);        
        GridBuildingSystem.Instance.OnObjectPlaced += GridBuildingSystem_OnObjectPlaced;
        GridBuildingSystem.Instance.OnReturnPlaceObjectToInventory += GridBuildingSystem_OnReturnPlaceObjectToInventory;
    }

    public override void ShowPopup(object param = null)
    {
        base.ShowPopup(param);

        _tabsPanel.TabChanged += OnChangeTab;
        for (int i = 0; i < _listItem.Count; i++)
        {
            _listItem[i].ItemSelected += PlacingItem;
        }
        //sub switch tab here
        FillInventory(_selectedTab.TabType);
        GridBuildingSystem.Instance.FireOnBuildingStartEvent();
        UIHUDManager.Instance.HideAllUIElement();

    }

    public override void HidePopup(object param = null)
    {
        base.HidePopup(param);

        Param paramData = param as Param;
        if (paramData != null)
        {
            isPlacingObject = paramData.isPlacingObject;
        }

        _tabsPanel.TabChanged -= OnChangeTab;

        for (int i = 0; i < _listItem.Count; i++)
        {
            _listItem[i].ItemSelected -= PlacingItem;
        }

        //unsub switch tab here
        if (!isPlacingObject)
        {
            GridBuildingSystem.Instance.FireOnBuildingEndEvent();
            UIHUDManager.Instance.ShowAllUIElement();

        }

    }

    public void FillInventory(InventoryTabType _selectedTabType = InventoryTabType.Counter)
    {
        _selectedTab = inventoryTabDatabase.GetTabByType(_selectedTabType);
        if (_selectedTab == null)
        {
            _selectedTab = inventoryTabDatabase.TabTypesList[0];
        }

        if (_selectedTab != null)
        {
            List<ItemStack> listItemsToShow = new List<ItemStack>();
            listItemsToShow = GameManager.Instance.GameData.InventoryData.Items.FindAll(o => o.Item.TabType == _selectedTab.TabType);

            FillInvetoryItems(listItemsToShow);
        }
        else
        {
            Debug.LogError("There's no selected tab");
        }
    }

    void FillInvetoryItems(List<ItemStack> listItemsToShow)
    {
        if (_listItem == null)
            _listItem = new List<UIInventoryItem>();

        int maxCount = Mathf.Max(listItemsToShow.Count, _listItem.Count);
        int diffCount = Mathf.Abs(listItemsToShow.Count - _listItem.Count);
        selectedItemId = -1;
        if( diffCount > 0 && _listItem.Count < listItemsToShow.Count)
        {
            for (int i = 0; i < diffCount; i++)
            {
                CreateInventoryItem(null);
            }
        }
        for (int i = 0; i < maxCount; i++)
        {
            if (i < listItemsToShow.Count)
            {
                _listItem[i].SetItem(listItemsToShow[i], false);

            }
            else 
            {
                _listItem[i].SetInactiveItem();
            }

        }


    }
    private void CreateInventoryItem(ItemStack prefabSO)
    {
        var item = Instantiate(_itemPrefab, _contentParent.transform);
        item.ItemSelected += PlacingItem;
        item.SetItem(prefabSO, false);
        item.gameObject.SetActive(true);
        _listItem.Add(item);
    }

    public void PlacingItem(PlacedObjectTypeSO itemToInspect)
    {
        if (_listItem.Exists(o => o.PlacedObjectTypeSO == itemToInspect))
        {
            int itemIndex = _listItem.FindIndex(o => o.PlacedObjectTypeSO == itemToInspect);

            selectedItemId = itemIndex;
        }

        GameManager.Instance.GameData.RemoveInventoryData(itemToInspect.Guid);
        GameManager.Instance.SaveGame();

        GridBuildingSystem.Instance.SetPlacedObjectTypeSO(_listItem[selectedItemId].PlacedObjectTypeSO,-Vector3.one);
        isPlacingObject = true;
        HidePopup();
    }


    void OnChangeTab(InventoryTab inventoryTab)
    {
        FillInventory(inventoryTab.TabType);
        _tabsPanel.SetTabs(inventoryTab);
    }
    private void GridBuildingSystem_OnObjectPlaced(object sender, EventArgs e)
    {
        if (selectedItemId >= 0 && selectedItemId < _listItem.Count)
        {
            if (selectedItemId >= 0)
            {
                selectedItemId = -1;
            }

        }

        FillInventory(_selectedTab.TabType);
    }
    private void GridBuildingSystem_OnReturnPlaceObjectToInventory(object sender, PlacedObjectTypeSO e)
    {
        FillInventory(_selectedTab.TabType);

    }

    public void SaveGrid()
    {
        GridBuildingSystem.Instance.SaveGrid();
    }
    public void ClosePopup()
    {
        isPlacingObject = false;
        HidePopup();
    }
}
