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
        [SerializeField] private Rewarder _rewarder;
        [SerializeField] private Pauser _pauser;

        public event Action<int> Win;
        public event Action<int> Failed;

        private void Start()
        {
            _fillOrchestrator.FillCompleted += OnFillCompleted;
            _fillOrchestrator.StartFill();
            _rewarder.RewardGranted += OnRewardGranted;
        }

        private void OnDisable()
        {
            _fillOrchestrator.FillCompleted -= OnFillCompleted;
            _rewarder.RewardGranted -= OnRewardGranted;
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
                _rewarder.RewardWin(percent);
            }
            else
            {
                _rewarder.RewardLose(percent);
            }
        }

        private void OnRewardGranted(int amount, bool isWin)
        {
            if (isWin)
            {
                Win?.Invoke(amount);
            }
            else
            {
                Failed?.Invoke(amount);
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