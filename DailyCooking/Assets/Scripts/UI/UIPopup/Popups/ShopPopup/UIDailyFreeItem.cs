using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIDailyFreeItem : MonoBehaviour
{
    [SerializeField] private UIShopPopup _shopView = null;

    [Header("Info Item")]
    [SerializeField] private TextMeshProUGUI txtAmount = null;
    [SerializeField] private GameObject bgDisable = null;
    [SerializeField] private Image imgIcon = null;

    [SerializeField] private Sprite coinSprite;
    [SerializeField] private Sprite gemSprite;
    private DailyFreeCurrency configItem = null;
    private int amount = 0;

    public void Setup(int id)
    {
        if(id> ShopManager.Instance.DailyFreeCurrency.Count)
        {
            gameObject.SetActive(false);
            return;
        }

        var item = ShopManager.Instance.DailyFreeCurrency[id];
        configItem = item;
        SetBgDisable(IsItemDisabled());

        switch (configItem.Id)
        {
            case ShopItemType.Gem:
                imgIcon.sprite = gemSprite;
                amount = int.Parse(configItem.Reward);
                txtAmount.text = MathUtil.NumberFormat(amount);
                break;
            case ShopItemType.Coin:
                imgIcon.sprite = coinSprite;
                amount = int.Parse(configItem.Reward);
                txtAmount.text = MathUtil.NumberFormat(amount);
                break;
        }
    }

    public void OnButtonClick()
    {
        AdsType type = GetAdsTypeFromType(configItem.Id);

        if (IsItemDisabled())
        {
            UIManager.Instance.ShowAlertMessage("Ads is Unavailable");
            return;
        }
        if (!AdsManager.Instance.IsRewardedAdsLoaded())
        {
            UIManager.Instance.ShowAlertMessage("Ads is Unavailable");
            return;
        }

        AdsManager.Instance.ShowRewardedAds(type.ToString(), () =>
        {
            UIPopupManager.Instance.ShowPopup(UIPopupType.UIRewardPopup, new UIRewardPopup.Param
            {
                reward = new RewardData[]{ 
                    new RewardData(configItem.Id.ToString(),amount) 
                }
            });
            GameManager.Instance.GameData.IncreaseShopDailyFreeWatchCount(configItem.Id);
            _shopView.SetupPopup();
        });
        
    }

    private AdsType GetAdsTypeFromType(ShopItemType type)
    {
        switch (type)
        {
            case ShopItemType.Gem:
                return AdsType.Free_Gem;
            case ShopItemType.Coin:
                return AdsType.Free_Cash;
            default:
                Debug.Log("not found type");
                return AdsType.Unknown;
        }
    }

    private void SetBgDisable(bool value)
    {
        if (bgDisable != null)
            bgDisable.SetActive(value);
    }

    public bool IsItemDisabled()
    {
        return GameManager.Instance.GameData.GetShopDailyFreeWatchCount(configItem.Id) >= configItem.Count;
    }

}
