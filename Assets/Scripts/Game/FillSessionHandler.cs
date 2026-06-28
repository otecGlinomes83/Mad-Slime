using ShapeFill;
using System;
using UnityEngine;
using YG;

namespace Game
{
    public class FillSessionHandler : MonoBehaviour
    {
        [SerializeField] private ShapeFillOrchestrator _fillOrchestrator;
        [SerializeField] private LevelTransitor _levelTransitor;
        [SerializeField] private Pauser _pauser;

        public event Action Win;
        public event Action Failed;

        private void Start()
        {
            _fillOrchestrator.FillCompleted += OnFillCompleted;
            _fillOrchestrator.StartFill();
        }

        private void OnDisable()
        {
            _fillOrchestrator.FillCompleted -= OnFillCompleted;
        }

        public void LoadNextLevel()
        {
            _levelTransitor.LoadNext();
            ClearInfo();
        }

        public void LoadPreviousLevel()
        {
            _levelTransitor.LoadPrevious();
            ClearInfo();
        }

        private void OnFillCompleted(float percent)
        {
            if (percent >= 1f)
            {
                Win?.Invoke();
            }
            else
            {
                Failed?.Invoke();
            }
        }

        private void ClearInfo()
        {
            YG2.saves.TargetQuotaCount = 0;
            YG2.saves.QuotaCount = 0;
            YG2.saves.DefaultCount = 0;
        }
    }
}