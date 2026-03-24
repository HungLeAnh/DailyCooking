using System;

[Serializable]
public class ShopData
{
    [System.NonSerialized]
    public Action OnResourceChange;

    private DateTime _dateLastRefreshShopDailyFree;
    private int dailyFreeItemCoinCount = 0;
    private int dailyFreeItemGemCount = 0;

    public int DailyFreeItemCoinCount { get => dailyFreeItemCoinCount; set => dailyFreeItemCoinCount = value; }
    public int DailyFreeItemGemCount { get => dailyFreeItemGemCount; set => dailyFreeItemGemCount = value; }
    public DateTime DateLastRefreshShopDailyFree { get => _dateLastRefreshShopDailyFree; set => _dateLastRefreshShopDailyFree = value; }

    public void UpdateDailyFreeItemCoinCount()
    {
        dailyFreeItemCoinCount += 1;
        OnResourceChange?.Invoke();
    }
    public void UpdateDailyFreeItemGemCount()
    {
        dailyFreeItemGemCount += 1;
        OnResourceChange?.Invoke();
    }
    public bool RefreshDailyShopOffer()
    {
        if (GameManager.Instance.GameData.ShopData.DateLastRefreshShopDailyFree < DateTime.UtcNow)
        {
            GameManager.Instance.GameData.ShopData.DateLastRefreshShopDailyFree = DateTime.UtcNow;
            dailyFreeItemCoinCount = 0;
            dailyFreeItemGemCount = 0;
            return true;
        }
        else
            return false;
    }
}

