using Scriptables;
using System;
using UnityEngine;

namespace Game
{
    public sealed class Rewarder : MonoBehaviour
    {
        [SerializeField] private RewardConfig _config;
        [SerializeField] private Wallet _wallet;

        public event Action<int, bool> RewardGranted;

        private void Awake()
        {
            if (_config == null)
            {
                throw new InvalidOperationException(
                    $"{name}: RewardConfig is not assigned. Create a RewardConfig asset and drag it into the _config field.");
            }

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

            int reward = _config.BaseReward;

            if (percentage > _config.WinFullMultiplierThreshold)
            {
                reward = Mathf.RoundToInt(_config.BaseReward * percentage);
            }

            RewardGranted?.Invoke(reward, true);
            _wallet.Add(reward);
        }

        public void RewardLose(float percentage)
        {
            int reward = 0;

            if (percentage >= _config.LoseMultiplierThreshold)
            {
                reward = _config.BaseReward / _config.LoseRewardDivisor;
            }

            RewardGranted?.Invoke(reward, false);

            if (reward > 0)
            {
                _wallet.Add(reward);
            }
        }
    }
}
