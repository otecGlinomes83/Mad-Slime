using Scriptables;
using System;
using UnityEngine;
using VContainer;

namespace Game
{
    public class Rewarder : MonoBehaviour
    {
        [SerializeField] private RewardConfig _config;

        private Wallet _wallet;
        private int _grantedAmount;
        private bool _isLevelResultCommitted;

        public int GrantedTotal => _grantedAmount;

        private bool _isWin;

        public bool IsWin => _isWin;

        [Inject]
        public void Construct(Wallet wallet)
        {
            _wallet = wallet;
        }

        private void Awake()
        {
            if (_config == null)
            {
                throw new InvalidOperationException(
                    $"{name}: RewardConfig is not assigned. Create a RewardConfig asset and drag it into the _config field.");
            }

            if (_config.LoseRewardDivisor <= 0)
            {
                throw new InvalidOperationException(
                    $"{name}: RewardConfig '{_config.name}' has LoseRewardDivisor <= 0. Set a positive divisor in the asset.");
            }

            if (_wallet == null)
            {
                throw new InvalidOperationException(
                    $"{name}: Wallet was not injected. Check that FillLifetimeScope registers Wallet and Rewarder.");
            }
        }

        public void CommitLevelResult(float percent)
        {
            if (_isLevelResultCommitted == true)
            {
                throw new InvalidOperationException(
                    $"{name}: CommitLevelResult is called twice for the same level. The result is already committed.");
            }

            if (percent < 0f || percent > 1f)
            {
                throw new ArgumentOutOfRangeException(nameof(percent),
                    "CommitLevelResult requires a fill percent within 0..1.");
            }

            _isLevelResultCommitted = true;
            _isWin = percent >= 1f;
            _grantedAmount = CalculateReward(percent);

            if (_grantedAmount > 0)
            {
                _wallet.Add(_grantedAmount);
            }
        }

        public void CommitRescueTopUp()
        {
            if (_isLevelResultCommitted == false)
            {
                throw new InvalidOperationException(
                    $"{name}: CommitRescueTopUp requires a committed level result. Commit the level result first.");
            }

            if (IsWin == true)
            {
                return;
            }

            _isWin = true;

            int topUp = _config.BaseReward - _grantedAmount;

            if (topUp > 0)
            {
                _wallet.Add(topUp);
            }

            _grantedAmount = _config.BaseReward;
        }

        public void ResetLevelResult()
        {
            _isLevelResultCommitted = false;
            _isWin = false;
            _grantedAmount = 0;
        }

        private int CalculateReward(float percent)
        {
            if (percent >= 1f)
            {
                return _config.BaseReward;
            }

            if (percent >= _config.LoseMultiplierThreshold)
            {
                return _config.BaseReward / _config.LoseRewardDivisor;
            }

            return 0;
        }
    }
}
