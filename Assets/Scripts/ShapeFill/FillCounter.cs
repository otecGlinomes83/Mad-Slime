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

        public int CalculateFill(int maxCubes)
        {
            float percent = _levelProgress.FillPercent;

            return Mathf.Clamp(Mathf.RoundToInt(percent * maxCubes), 0, maxCubes);
        }
    }
}
