using Scriptables;
using System;
using UnityEngine;
using YG;

namespace Game
{
    public sealed class AdScheduler : MonoBehaviour
    {
        [SerializeField] private YandexConfig _config;

        private YandexAdsBridge _bridge;
        private Action _pendingRewardAction;
        private Action _pendingRejectedAction;
        private bool _rewardedReceived;

        private void Awake()
        {
            if (_config == null)
            {
                throw new InvalidOperationException(
                    $"{name}: YandexConfig is not assigned. Create a YandexConfig asset and drag it into the _config field.");
            }

            if (_config.InterstitialEveryLevels <= 0)
            {
                throw new InvalidOperationException(
                    $"{name}: YandexConfig Interstitial Every Levels must be greater than zero.");
            }
        }

        public void Setup(YandexAdsBridge bridge)
        {
            _bridge = bridge ?? throw new ArgumentNullException(nameof(bridge));
        }

        private void OnEnable()
        {
            if (_bridge == null)
            {
                return;
            }

            Subscribe();
        }

        private void OnDisable()
        {
            if (_bridge == null)
            {
                return;
            }

            Unsubscribe();
        }

        public void ShowInterstitialIfNeeded(int levelNumber)
        {
            if (levelNumber % _config.InterstitialEveryLevels != 0)
            {
                return;
            }

            TryShowInterstitial();
        }

        public void TryShowInterstitial()
        {
            if (YG2.nowAdsShow == true)
            {
                return;
            }

            _bridge.ShowInterstitial();
        }

        public void ShowDoubleReward(Action onGranted)
        {
            ShowRewarded(_config.DoubleRewardId, onGranted);
        }

        public void ShowRewarded(string rewardId, Action onGranted)
        {
            ShowRewarded(rewardId, onGranted, null);
        }

        public void ShowRewarded(string rewardId, Action onGranted, Action onRejected)
        {
            if (onGranted == null)
            {
                throw new ArgumentNullException(nameof(onGranted));
            }

            if (YG2.nowAdsShow == true)
            {
                onRejected?.Invoke();
                return;
            }

            _rewardedReceived = false;
            _pendingRewardAction = onGranted;
            _pendingRejectedAction = onRejected;
            _bridge.ShowRewarded(rewardId);
        }

        private void Subscribe()
        {
            _bridge.RewardedOpened += OnRewardedOpened;
            _bridge.RewardedReceived += OnRewardedReceived;
            _bridge.RewardedClosed += OnRewardedClosed;
            _bridge.RewardedError += OnRewardedError;
        }

        private void Unsubscribe()
        {
            _bridge.RewardedOpened -= OnRewardedOpened;
            _bridge.RewardedReceived -= OnRewardedReceived;
            _bridge.RewardedClosed -= OnRewardedClosed;
            _bridge.RewardedError -= OnRewardedError;
        }

        private void OnRewardedOpened(string rewardId)
        {
            _rewardedReceived = false;
        }

        private void OnRewardedReceived(string rewardId)
        {
            _rewardedReceived = true;
        }

        private void OnRewardedClosed(string rewardId)
        {
            Action action = _pendingRewardAction;
            Action rejected = _pendingRejectedAction;
            _pendingRewardAction = null;
            _pendingRejectedAction = null;

            if (_rewardedReceived == true)
            {
                action?.Invoke();
            }
            else
            {
                rejected?.Invoke();
            }
        }

        private void OnRewardedError(string rewardId)
        {
            Action rejected = _pendingRejectedAction;
            _pendingRewardAction = null;
            _pendingRejectedAction = null;
            rejected?.Invoke();
        }
    }
}
