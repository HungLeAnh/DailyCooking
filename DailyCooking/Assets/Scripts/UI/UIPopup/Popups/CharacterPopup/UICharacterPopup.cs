using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UICharacterPopup : UIPopup, IDragHandler
{
    private static string TITLE = "BUILD";
    public class Param
    {
        public bool isPlacingObject;
    }
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private Button backButton;
    [SerializeField] private TabController tabController;  
    [SerializeField] private Customization_Data customizationData;
    [SerializeField] private GameObject characterObject;
    [SerializeField] private List<CustomizationPart> customizationParts;
    [SerializeField] private GameObject tabPrefab;

    private InventoryTab _selectedTab;
    private int selectedItemId = -1;
    private List<UIInventoryItem> listItem = new List<UIInventoryItem>();
    private bool isPlacingObject = false;
    private bool isShowItems = false;

    public Button BackButton => backButton;
    private void Awake()
    {
        tabPrefab.SetActive(false);
        var listTabNames = customizationData.CosmeticDatas.Select(x => x.Type).ToList();
        var listTabIcons = customizationData.CosmeticDatas.Select(x => (x.Icon)).ToList();
        foreach ( var icon in listTabIcons)
        {
            var instantiatedTab = Instantiate(tabPrefab, tabController.transform);
            instantiatedTab.SetActive(true);
            var tab = instantiatedTab.GetComponent<TabPair>();
            
            tabController.tabs.Add(tab);
        }
        tabController.InitializeTabs(listTabNames, listTabIcons);

        backButton.onClick.AddListener(() =>
        {
            if (!isShowItems)
            {
                ClosePopup();

            }
            else
            {
                isShowItems = false;
                titleText.text = TITLE;
                
            }
        });
        titleText.text = TITLE;
    }
    
    public override void SetupPopup()
    {
        base.SetupPopup();

    }   

    public override void ShowPopup(object param = null)
    {
        base.ShowPopup(param);
        //Debug.Log(listItem.Count + " Object Subscribe ItemSelected");
        //Debug.Log("Start building");

        for (int i = 0; i < listItem.Count; i++)
        {

        }
    }

    public override void HidePopup(object param = null)
    {
        base.HidePopup(param);
        //Debug.Log("Unsubscribe ItemSelected");

        Param paramData = param as Param;
        if (paramData != null)
        {
            isPlacingObject = paramData.isPlacingObject;
        }

        for (int i = 0; i < listItem.Count; i++)
        {

        }

        
        if (!isPlacingObject)
        {
            GridBuildingSystem.Instance.BuildingPlacementManager.FireOnBuildingEndEvent();
            UIHUDManager.Instance.ShowAllUIElement();
            GameManager.Instance.ShowJoyStick();
        }

    }

    public void FillInventory(InventoryTabType _selectedTabType = InventoryTabType.Counter)
    {
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
        if (listItem == null)
            listItem = new List<UIInventoryItem>();

        int maxCount = Mathf.Max(listItemsToShow.Count, listItem.Count);
        int diffCount = Mathf.Abs(listItemsToShow.Count - listItem.Count);
        selectedItemId = -1;
        if( diffCount > 0 && listItem.Count < listItemsToShow.Count)
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
                listItem[i].SetItem(listItemsToShow[i], false);

            }
            else 
            {
                listItem[i].SetInactiveItem();
            }

        }


    }
    private void CreateInventoryItem(ItemStack prefabSO)
    {

    }

    private void OnChangeTab(InventoryTab inventoryTab)
    {
        isShowItems = true;
        titleText.text = inventoryTab.TabType.ToString();
        FillInventory(inventoryTab.TabType);
        //_tabsPanel.SetTabs(inventoryTab);
    }
    public void ClosePopup()
    {
        isPlacingObject = false;
        isShowItems = false;
        HidePopup();
    }

    public void OnDrag(PointerEventData eventData)
    {
        characterObject.transform.eulerAngles += new Vector3(0,-eventData.delta.x,0);
    }
}
