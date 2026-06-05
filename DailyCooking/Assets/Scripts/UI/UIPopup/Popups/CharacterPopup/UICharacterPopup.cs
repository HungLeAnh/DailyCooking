using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using static UnityEditor.PlayerSettings;

public class UICharacterPopup : UIPopup
{
    [SerializeField] private Button backButton;
    [SerializeField] private TabController tabController;
    [SerializeField] private TextMeshProUGUI currentTypeText;
    [SerializeField] private Button ClearCurrentTypeCustomButton;
    [SerializeField] private Button SaveCustomButtom;
    [SerializeField] private Transform UnfitParentTransform;
    [SerializeField] private GameObject UnfitPrefab;

    [SerializeField] private GameObject characterObject;
    [SerializeField] private List<CustomizationPart> customizationParts;
    [SerializeField] private GameObject tabPrefab;
    [SerializeField] private RectTransform tabContent;

    [SerializeField] private RectTransform itemContent;
    [SerializeField] private GameObject itemPrefab;

    private int selectedItemId = -1;
    private List<UICharacterItem> listItem = new List<UICharacterItem>();
    private bool isShowItems = false;

    private Cosmetic currentCosmetic;
    private CosmeticsData currentCosmeticsData;
    private CustomizationPart currentCustomizationPart;
    private List<string> unfittables = new();
    private List<CustomizationPart> parts = new();


    private Dictionary<string,Sprite> UnfitSpriteDictionary;

    public Button BackButton => backButton;
    private void Awake()
    {
        tabPrefab.SetActive(false);
        itemPrefab.SetActive(false);        
        
        UnfitSpriteDictionary = new Dictionary<string,Sprite>();
        
        foreach (var part in customizationParts)
        {
            part.Initialise(ConfigManager.Instance.CustomizationData);
        }

        foreach ( var tabData in ConfigManager.Instance.CustomizationData.CosmeticDatas)
        {
            var instantiatedTab = Instantiate(tabPrefab, tabContent);
            instantiatedTab.SetActive(true);
            var tab = instantiatedTab.GetComponent<TabPair>();
            
            tabController.tabs.Add(tab);

            UnfitSpriteDictionary[tabData.Type] = tabData.Icon;
        }
        var listTabNames = ConfigManager.Instance.CustomizationData.CosmeticDatas.Select(x => x.Type).ToList();
        var listTabIcons = ConfigManager.Instance.CustomizationData.CosmeticDatas.Select(x => (x.Icon)).ToList();

        tabController.onTabChanged += () => OnChangeTab(tabController.CurrentTab.name);
        tabController.InitializeTabs(listTabNames, listTabIcons);

        var playerData = GameManager.Instance.GameData.GetPlayerStatsById(SessionManager.Instance.PlayerId);
        foreach(var item in playerData.CharacterCustomizationIds)
        {
            var part = customizationParts.FirstOrDefault(x => x.Type == item.Key);
            if (part != null)
            {
                if(item.Value >= 0)
                    part.SetMesh(item.Value);
                else
                    part.Clear();
            }
        }

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
        ClearCurrentTypeCustomButton.onClick.AddListener(() =>
        {
            if (currentCosmeticsData != null)
            {
                currentCosmeticsData.OnClear?.Invoke();
            }
        });
        SaveCustomButtom.onClick.AddListener(() =>
        {
            SaveCustom();
        });
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
            var cosmeticsData = ConfigManager.Instance.CustomizationData.CosmeticDatas.Find(x => x.Type == type);
            SetCosmeticType(cosmeticsData);
            currentTypeText.text = type;
            foreach (Transform child in UnfitParentTransform)
            {
                if (child.gameObject == UnfitPrefab) 
                    continue;
                Destroy(child.gameObject);
            }
            if(unfittables != null|| unfittables.Count > 0)
            {
                //Debug.Log("unfit count: " + unfittables.Count);
                foreach(var unfit in unfittables)
                {
                    if (string.IsNullOrEmpty(unfit)) break;

                    //Debug.Log("unfit: " + unfit);
                    var instance = Instantiate(UnfitPrefab, UnfitParentTransform);
                    instance.GetComponent<Image>().sprite = UnfitSpriteDictionary[unfit];
                    instance.gameObject.SetActive(true);
                }

            }
    
            FillInvetoryItems(cosmeticsData.Cosmetics);
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

        //Debug.Log("Fill Inventory with " + listItemsToShow.Count + " items");
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
        //Debug.Log("List Item Count: " + listItem.Count);
        for (int i = 0; i < maxCount; i++)
        {
            if (i < listItemsToShow.Count)
            {
                if (listItemsToShow[i] != null && listItem[i] != null)
                {
                    bool isLocked = !GameManager.Instance.GameData.CosmeticData.IsCosmeticUnlocked(currentCosmeticsData.Type, i);
                    listItem[i].SetItem(i, listItemsToShow[i], false, isLocked);
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
        item.ItemUnlocked += OnUnlockItem;

        listItem.Add(item);
    }

    private void OnUnlockItem(int index)
    {
        string itemName = currentCosmeticsData.Cosmetics[index].Name;
        UIPopupManager.Instance.ShowPopup(UIPopupType.UIGameConfirmPopup, new UIGameConfirmPopup.Param
        {
            Title = "Unlock Cosmetic",
            Message = $"Are you sure you want to unlock {itemName}?",
            YesAction = () =>
            {
                GameManager.Instance.GameData.CosmeticData.UnlockCosmetic(currentCosmeticsData.Type, index);
                GameManager.Instance.SaveGame();
                FillInvetoryItems(currentCosmeticsData.Cosmetics);
            }
        });
    }

    private void OnSelectItem(int index)
    {
        currentCosmeticsData.SetMesh(index);
    }
    private void SetCosmeticType(CosmeticsData data)
    {
        currentCosmeticsData = data;
        unfittables.Clear();
        unfittables = currentCosmeticsData.UnfittableTypes
           .Split(',')
           .Select(s => s.Trim())
           .ToList();

        if (customizationParts == null)
            Debug.Log("customizationParts: null");
        if(currentCosmeticsData == null)
            Debug.Log("currentCosmeticData: null");
        if (customizationParts[0].CosmeticsData == null)
            Debug.Log("customizationPart's cosmeticData: null");

        currentCustomizationPart = customizationParts.FirstOrDefault(x => x.Type == currentCosmeticsData.Type);

        currentCosmetic = currentCustomizationPart.CurrentCosmetic;

        parts.Clear();

        foreach (var part in customizationParts)
        {
            parts.Add(part);
        }

        parts.Remove(currentCustomizationPart);
    }
    private void SaveCustom()
    {
        var playerData = GameManager.Instance.GameData.GetPlayerStatsById(SessionManager.Instance.PlayerId);
        foreach (var part in customizationParts)
        {
            Debug.Log($"Saving {part.Type} with index {part.Index}");
            playerData.UpdatePlayerCustomization(part.Type, part.Index);
        }

    }
    private void OnChangeTab(string type)
    {
        //Debug.Log("Type: " + type);
        isShowItems = true;
        FillInventory(type);
    }
    public void ClosePopup()
    {
        isShowItems = false;
        HidePopup();
    }


}
