// Pure economy formulas shared by the server logic (GameManager.Economy) and the UI.
public static class EconomyRules
{
    private const int LEVEL_UP_COINS_PER_LEVEL = 100;

    public static int GetLevelUpRewardCoins(int level) => level * LEVEL_UP_COINS_PER_LEVEL;
}
