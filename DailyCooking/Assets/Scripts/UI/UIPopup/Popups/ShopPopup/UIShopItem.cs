using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIShopItem : MonoBehaviour
{
    [SerializeField] private Image imageIcon;
    [SerializeField] private TextMeshProUGUI textName;
    [SerializeField] private TextMeshProUGUI textPrice;
    [SerializeField] private Transform lockTransform;
    [SerializeField] private Button buttonBuy;
    private ConfigShopItem configShopItem;
    private ShopItemCategory itemCategory;
    private Dictionary<string, int> parsedData = new Dictionary<string, int>();

    private void OnDestroy()
    {
        if (GameManager.Instance == null)
            return;
        GameManager.Instance.GameData.PlayerStats.OnLevelChange += OnLevelChanged;
    }
    public void SetItem(ConfigShopItem item,ShopItemCategory itemCategory)
    {
        this.gameObject.SetActive(true);    
        this.configShopItem = item;
        this.itemCategory = itemCategory;

        GameManager.Instance.GameData.PlayerStats.OnLevelChange += OnLevelChanged;

        //imageIcon.sprite = item.Icon;
        textName.text = item.Name;
        textPrice.text = item.Price.ToString();
        buttonBuy.onClick.AddListener(OnClickButtonBuy);
        if(item.UnlockLevel > GameManager.Instance.GameData.PlayerStats.playerData.Level)
        {
            lockTransform.gameObject.SetActive(true);
        }
        else
        {
            lockTransform.gameObject.SetActive(false);
        }

        string[] pairs = item.Reward.Split(';');

        foreach (var pair in pairs)
        {
            // Split each pair by '_' to get id and amount
            string[] parts = pair.Split('_');
            if (parts.Length == 2)
            {
                string id = parts[0];
                int amount = int.Parse(parts[1]); 
                parsedData[id] = amount;
            }
        }
        GetReward(parsedData);

    }

    private void OnLevelChanged()
    {
        if (configShopItem.UnlockLevel > GameManager.Instance.GameData.PlayerStats.playerData.Level)
        {
            lockTransform.gameObject.SetActive(true);
        }
        else
        {
            lockTransform.gameObject.SetActive(false);
        }
    }

    private void OnClickButtonBuy()
    {
        ShopManager.Instance.OnPurchase(configShopItem, parsedData);
    }

    private void GetReward(Dictionary<string, int> parsedData)
    {
        var parsedDataList = parsedData.ToList();
        if (parsedDataList.Count == 0) return;
        switch (itemCategory)
        {
            case ShopItemCategory.Counters :
            case ShopItemCategory.Tables:
            case ShopItemCategory.Walls:

                var placedObject = GridBuildingSystem.Instance.PlacedObjectDatabase.PlacedObjects
                    .Find(x => x.Guid == parsedDataList[0].Key);
                if (placedObject == null)
                {
                    gameObject.SetActive(false);
                    break;
                }

                SetIcon(placedObject.icon);

                break;
            default:
                break;
        }
    }

    private void SetIcon(Sprite icon)
    {
        this.imageIcon.sprite = icon;
    }
}