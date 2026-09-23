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
    // Resets the daily free counts once per UTC day. Returns true when it reset.
    // The server's (host's) clock decides for claims; clients only use this for display.
    public bool RefreshDailyShopOffer()
    {
        return RefreshDailyShopOffer(DateTime.UtcNow);
    }
    public bool RefreshDailyShopOffer(DateTime utcNow)
    {
        if (_dateLastRefreshShopDailyFree.Date < utcNow.Date)
        {
            _dateLastRefreshShopDailyFree = utcNow;
            dailyFreeItemCoinCount = 0;
            dailyFreeItemGemCount = 0;
            OnResourceChange?.Invoke();
            return true;
        }
        return false;
    }
}

