using UnityEngine;
using YG;

public sealed class FillCounter : MonoBehaviour
{
    [SerializeField] private int _defaultCountDivisor;

    public int CalculateFill(int maxCubes)
    {
        int quotaCount = YG2.saves.QuotaCount;
        int defaultCount = YG2.saves.DefaultCount;
        int totalTarget = YG2.saves.TargetQuotaCount;
        int overage = 0;
        float percentage = 0f;

        defaultCount /= _defaultCountDivisor;
        quotaCount += defaultCount;

        if (totalTarget > 0 && quotaCount > totalTarget)
        {
            overage = quotaCount - totalTarget;
            YG2.saves.Balance += overage;
        }

        if (totalTarget > 0)
        {
            percentage = (float)quotaCount / totalTarget;
        }

        return Mathf.Clamp(Mathf.RoundToInt(percentage * maxCubes), 0, maxCubes);
    }
}