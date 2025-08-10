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

    public void SetItem(ConfigShopItem item,ShopItemCategory itemCategory)
    {
        this.gameObject.SetActive(true);    
        this.configShopItem = item;
        this.itemCategory = itemCategory;

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

        ProcessRewardVisuals(item.Reward);

    }

    private void OnClickButtonBuy()
    {
        ShopManager.Instance.OnPurchase(configShopItem);
    }

    private void ProcessRewardVisuals(RewardData[] rewardData)
    {
        if(rewardData.Length == 0) return;
        switch (itemCategory)
        {
            case ShopItemCategory.Counters:

                var placedObject = GridBuildingSystem.Instance.PlacedObjectDatabase.PlacedObjects
                    .Find(x => x.id == rewardData[0].id);
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