using Quota;
using UnityEngine;
using VContainer;

namespace ShapeFill
{
    public class FillResultCalculator : MonoBehaviour
    {
        private QuotaCounter _quotaCounter;

        [Inject]
        public void Construct(QuotaCounter quotaCounter)
        {
            _quotaCounter = quotaCounter;
        }

        public FillResult Calculate(int requiredCubes)
        {
            int quotaCubes = CalculateQuotaFill(requiredCubes);
            int bonusCubes = CalculateBonusFill(requiredCubes, quotaCubes);

            return new FillResult(quotaCubes, bonusCubes, requiredCubes);
        }

        private int CalculateQuotaFill(int maxCubes)
        {
            if (_quotaCounter.TotalQuotaTarget <= 0)
            {
                return 0;
            }

            float quotaPercent = Mathf.Clamp01(_quotaCounter.CollectedQuotaCount / (float)_quotaCounter.TotalQuotaTarget);

            return Mathf.Clamp(Mathf.RoundToInt(quotaPercent * maxCubes), 0, maxCubes);
        }

        private int CalculateBonusFill(int maxCubes, int quotaCubes)
        {
            int totalCount = Mathf.Clamp(Mathf.RoundToInt(_quotaCounter.GetFillPercent() * maxCubes), 0, maxCubes);

            return Mathf.Max(0, totalCount - quotaCubes);
        }
    }
}
