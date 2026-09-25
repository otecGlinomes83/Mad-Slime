using UnityEngine;
using VContainer;

namespace ShapeFill
{
    public sealed class FillCounter : MonoBehaviour
    {
        private Game.LevelProgress _levelProgress;

        [Inject]
        public void Construct(Game.LevelProgress levelProgress)
        {
            _levelProgress = levelProgress;
        }

        public int CalculateQuotaFill(int maxCubes)
        {
            return Mathf.Clamp(Mathf.RoundToInt(GetQuotaPercent() * maxCubes), 0, maxCubes);
        }

        public int CalculateBonusFill(int maxCubes)
        {
            int quotaCount = CalculateQuotaFill(maxCubes);
            int totalCount = Mathf.Clamp(Mathf.RoundToInt(_levelProgress.FillPercent * maxCubes), 0, maxCubes);

            return Mathf.Max(0, totalCount - quotaCount);
        }

        private float GetQuotaPercent()
        {
            if (_levelProgress.TotalQuotaTarget <= 0)
            {
                return 0f;
            }

            return Mathf.Clamp01(_levelProgress.CollectedQuotaCount / (float)_levelProgress.TotalQuotaTarget);
        }
    }
}
