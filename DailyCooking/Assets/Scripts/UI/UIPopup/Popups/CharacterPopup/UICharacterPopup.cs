using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UICharacterPopup : UIPopup
{
    [SerializeField] private Button backButton;
    [SerializeField] private TabController tabController;  
    [SerializeField] private Customization_Data customizationData;
    [SerializeField] private GameObject characterObject;
    [SerializeField] private List<CustomizationPart> customizationParts;
    [SerializeField] private GameObject tabPrefab;
    [SerializeField] private RectTransform tabContent;

    [SerializeField] private RectTransform itemContent;
    [SerializeField] private GameObject itemPrefab;

    private int selectedItemId = -1;
    private List<UICharacterItem> listItem = new List<UICharacterItem>();
    private bool isPlacingObject = false;
    private bool isShowItems = false;

    private Cosmetic currentCosmetic;
    private CosmeticsData currentCosmeticsData;
    private CustomizationPart currentCustomizationPart;
    private List<string> _unfittables = new();


    public Button BackButton => backButton;
    private void Awake()
    {
        tabPrefab.SetActive(false);
        itemPrefab.SetActive(false);
        var listTabNames = customizationData.CosmeticDatas.Select(x => x.Type).ToList();
        var listTabIcons = customizationData.CosmeticDatas.Select(x => (x.Icon)).ToList();
        foreach ( var icon in listTabIcons)
        {
            var instantiatedTab = Instantiate(tabPrefab, tabContent);
            instantiatedTab.SetActive(true);
            var tab = instantiatedTab.GetComponent<TabPair>();
            
            tabController.tabs.Add(tab);
        }
        tabController.onTabChanged += () => OnChangeTab(tabController.CurrentTab.name);
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
            }
        });
        foreach (var part in customizationParts)
        {
            part.Initialise(customizationData);
        }
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

    }

    public override void HidePopup(object param = null)
    {
        base.HidePopup(param);
        //Debug.Log("Unsubscribe ItemSelected");

        for (int i = 0; i < listItem.Count; i++)
        {

        }

    }

    public void FillInventory(string type)
    {
        if (tabController.CurrentTab != null)
        {
            var listItemsToShow = customizationData.CosmeticDatas.Find(x => x.Type == type).Cosmetics;

            FillInvetoryItems(listItemsToShow);
        }
        else
        {
            Debug.LogError("There's no selected tab");
        }
    }

    void FillInvetoryItems(List<Cosmetic> listItemsToShow)
    {
        if (listItem == null)
            listItem = new List<UICharacterItem>();
        if (listItemsToShow == null)
        {
            Debug.LogError($"[UICharacterPopup] listItemsToShow is null!");
            return;
        }

        Debug.Log("Fill Inventory with " + listItemsToShow.Count + " items");
        int maxCount = Mathf.Max(listItemsToShow.Count, listItem.Count);
        int diffCount = Mathf.Abs(listItemsToShow.Count - listItem.Count);
        selectedItemId = -1;
        if( diffCount > 0 && listItem.Count < listItemsToShow.Count)
        {
            for (int i = 0; i < diffCount; i++)
            {
                CreateInventoryItem();
            }
        }
        Debug.Log("List Item Count: " + listItem.Count);
        for (int i = 0; i < maxCount; i++)
        {
            if (i < listItemsToShow.Count)
            {
                if (listItemsToShow[i] != null && listItem[i] != null)
                {
                    listItem[i].SetItem(listItemsToShow[i], false);
                }
                else
                {
                    Debug.LogWarning("There's no cosmetic data for item " + i);
                    Debug.Log($"listItemsToShow {i} is null: {listItemsToShow[i] == null}");
                    Debug.Log($"list item {i} is null: {listItem[i] == null}");
                }
            }
            else
            {
                listItem[i].SetInactiveItem();
            }

        }
    }
    private void CreateInventoryItem()
    {
        var instantiatedItem = Instantiate(itemPrefab, itemContent);
        instantiatedItem.SetActive(true);
        var item = instantiatedItem.GetComponent<UICharacterItem>();
        item.ItemSelected += OnSelectItem;

        listItem.Add(item);
    }

    private void OnSelectItem()
    {
  
    }
    private void SetCosmeticType(CosmeticsData data)
    {
        currentCosmeticsData = data;

        _unfittables = currentCosmeticsData.UnfittableTypes
           .Split(',')
           .Select(s => s.Trim())
           .ToList();

        currentCustomizationPart = customizationParts.FirstOrDefault(x => x.Type == currentCosmeticsData.Type);

        currentCosmetic = currentCustomizationPart.CurrentCosmetic;
        var part = customizationParts.FirstOrDefault(x => x.Type == currentCosmeticsData.Type);
        if (part != null)
        {
            part.SetMesh(currentCosmetic);
        }
        else
        {
            Debug.LogError("There's no customization part for type " + currentCosmeticsData.Type);
        }
        //parts.Clear();

        //foreach (var part in customization.CustomizationParts)
        //{
        //    parts.Add(part);
        //}

        //parts.Remove(currentCustomizationPart);
    }
    private void OnChangeTab(string type)
    {
        isShowItems = true;
        FillInventory(type);
    }
    public void ClosePopup()
    {
        isPlacingObject = false;
        isShowItems = false;
        HidePopup();
    }


}
