using Scriptables;
using System;
using UnityEngine;
using YG;

namespace Game
{
    public sealed class AdScheduler : MonoBehaviour
    {
        [SerializeField] private YandexConfig _config;

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
        }

        private void OnEnable()
        {
            YG2.onOpenRewardedAdv += OnRewardedOpened;
            YG2.onRewardAdv += OnRewardReceived;
            YG2.onCloseRewardedAdv += OnRewardedClosed;
            YG2.onErrorRewardedAdv += OnRewardedError;
        }

        private void OnDisable()
        {
            YG2.onOpenRewardedAdv -= OnRewardedOpened;
            YG2.onRewardAdv -= OnRewardReceived;
            YG2.onCloseRewardedAdv -= OnRewardedClosed;
            YG2.onErrorRewardedAdv -= OnRewardedError;
        }

        public void TryShowInterstitial()
        {
            YG2.InterstitialAdvShow();
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

            if (YG2.nowAdsShow == true)
            {
                onRejected?.Invoke();
                return;
            }

            _rewardedReceived = false;
            _pendingRewardAction = onGranted;
            _pendingRejectedAction = onRejected;
            YG2.RewardedAdvShow(rewardId);
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
