using System.Linq;
using UnityEngine;

public class MathUtil
{
    public static string NumberFormat(int cash)
    {
        if (cash == 0) return "0";

        int absCash = Mathf.Abs(cash);
        string[] suffixes = { "", "k", "M", "B", "T" };

        int suffixIndex = (int)(Mathf.Log10(absCash + 0.00001f) / 3);

        suffixIndex = Mathf.Clamp(suffixIndex, 0, suffixes.Count() - 1);

        float dividor = Mathf.Pow(10, suffixIndex * 3);
        float shortValue = cash / dividor;

        string format = (shortValue % 1 == 0 || suffixIndex == 0) ? "F0" : "F1";

        return shortValue.ToString(format) + suffixes[suffixIndex];
    }
}
