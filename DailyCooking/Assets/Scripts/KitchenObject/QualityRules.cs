using System.Collections.Generic;
using UnityEngine;

// How stars turn into a dish rating and money. Server-side payment uses Payment().
public static class QualityRules
{
    public static int RoundHalfUp(float value) => Mathf.FloorToInt(value + 0.5f);

    // Average stars over the ingredients that went through a gesture step; 0 when none did.
    public static int DishStars(IEnumerable<QualityRecord> ingredients)
    {
        int sum = 0;
        int count = 0;
        if (ingredients != null)
        {
            foreach (QualityRecord record in ingredients)
            {
                if (record.Count == 0)
                    continue;
                sum += record.Stars;
                count++;
            }
        }
        return count == 0 ? 0 : RoundHalfUp((float)sum / count);
    }

    public static float TipPercent(int stars)
    {
        switch (stars)
        {
            case 2: return 0.10f;
            case 3: return 0.25f;
            default: return 0f;
        }
    }

    // Dish price, plus the wait-time tip, plus the quality tip — both tips on the base price.
    public static int Payment(int baseCash, float waitTipPercent, int dishStars)
    {
        return baseCash + (int)(baseCash * waitTipPercent) + (int)(baseCash * TipPercent(dishStars));
    }
}
