using System;
using UnityEngine;

namespace Game
{
    public sealed class Rewarder : MonoBehaviour
    {
        private const float WinFullMultiplierThreshold = 1.25f;
        private const float LoseMultiplierThreshold = 0.25f;
        private const int LoseRewardDivisor = 4;

        [SerializeField] private Wallet _wallet;
        [SerializeField] private int _baseReward = 50;

        public event Action<int, bool> RewardGranted;

        private void Awake()
        {
            if (_wallet == null)
            {
                throw new InvalidOperationException(
                    $"{name}: Rewarder requires Wallet to be assigned in the inspector.");
            }
        }

        public void RewardWin(float percentage)
        {
            if (percentage < 1f)
            {
                throw new ArgumentOutOfRangeException(nameof(percentage),
                    "RewardWin requires percentage >= 1.0 (Win means quota was completed).");
            }

            int reward = _baseReward;

            if (percentage > WinFullMultiplierThreshold)
            {
                reward = Mathf.RoundToInt(_baseReward * percentage);
            }

            RewardGranted?.Invoke(reward, true);
            _wallet.Add(reward);
        }

        public void RewardLose(float percentage)
        {
            int reward = 0;

            if (percentage >= LoseMultiplierThreshold)
            {
                reward = _baseReward / LoseRewardDivisor;
            }

            RewardGranted?.Invoke(reward, false);

            if (reward > 0)
            {
                _wallet.Add(reward);
            }
        }
    }
}