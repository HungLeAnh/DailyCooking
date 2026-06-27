using System.Collections.Generic;

[System.Serializable]
public class CosmeticData
{
    public Dictionary<string, List<int>> UnlockedCosmetics { get; private set; } = new Dictionary<string, List<int>>();
    
    public void UnlockCosmetic(string type, int index)
    {
        if (!UnlockedCosmetics.ContainsKey(type))
        {
            UnlockedCosmetics[type] = new List<int>();
        }

        if (!UnlockedCosmetics[type].Contains(index))
        {
            UnlockedCosmetics[type].Add(index);
        }
    }

    public bool IsCosmeticUnlocked(string type, int index)
    {
        if (UnlockedCosmetics.TryGetValue(type, out List<int> unlockedIndices))
        {
            return unlockedIndices.Contains(index);
        }
        return false;
    }
}
