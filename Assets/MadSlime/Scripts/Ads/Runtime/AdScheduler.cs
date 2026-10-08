using Core;
using Scriptables;
using System;
using UnityEngine;
using VContainer;

namespace Game
{
    public sealed class AdScheduler : MonoBehaviour
    {
        [SerializeField] private YandexConfig _config;

        private IAdsService _adsService;
        private Action _pendingRewardAction;
        private Action _pendingRejectedAction;
        private bool _rewardedReceived;

        [Inject]
        public void Construct(IAdsService adsService)
        {
            _adsService = adsService;
        }

        private void Awake()
        {
            if (_config == null)
            {
                throw new InvalidOperationException(
                    $"{name}: YandexConfig is not assigned. Create a YandexConfig asset and drag it into the _config field.");
            }

            if (_adsService == null)
            {
                throw new InvalidOperationException(
                    $"{name}: IAdsService was not injected. Check that ProjectLifetimeScope registers the YG2 ads adapter.");
            }
        }

        private void OnEnable()
        {
            _adsService.RewardedOpened += OnRewardedOpened;
            _adsService.RewardReceived += OnRewardReceived;
            _adsService.RewardedClosed += OnRewardedClosed;
            _adsService.RewardedError += OnRewardedError;
        }

        private void OnDisable()
        {
            _adsService.RewardedOpened -= OnRewardedOpened;
            _adsService.RewardReceived -= OnRewardReceived;
            _adsService.RewardedClosed -= OnRewardedClosed;
            _adsService.RewardedError -= OnRewardedError;
        }

        public void TryShowInterstitial()
        {
            _adsService.ShowInterstitial();
        }

        public string RouletteRewardId => _config.RouletteRewardId;

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

            if (_adsService.IsAdShowing == true)
            {
                onRejected?.Invoke();
                return;
            }

            _rewardedReceived = false;
            _pendingRewardAction = onGranted;
            _pendingRejectedAction = onRejected;
            _adsService.ShowRewarded(rewardId);
        }

        private void OnRewardedOpened()
        {
            _rewardedReceived = false;
        }

        private void OnRewardReceived(string rewardId)
        {
            _rewardedReceived = true;
        }

        private void OnRewardedClosed()
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

        private void OnRewardedError()
        {
            Action rejected = _pendingRejectedAction;
            _pendingRewardAction = null;
            _pendingRejectedAction = null;
            rejected?.Invoke();
        }
    }
}
