using Scriptables;
using System;
using UnityEngine;
using YG;

namespace Game
{
    public sealed class AdScheduler : MonoBehaviour
    {
        [SerializeField] private AdsConfig _config;

        private Action _pendingRewardAction;

        private void Awake()
        {
            if (_config == null)
            {
                throw new InvalidOperationException(
                    $"{name}: AdsConfig is not assigned. Create an AdsConfig asset and drag it into the _config field.");
            }

            if (_config.InterstitialEveryLevels <= 0)
            {
                throw new InvalidOperationException(
                    $"{name}: AdsConfig Interstitial Every Levels must be greater than zero.");
            }
        }

#if RewardedAdv_yg
        private void OnEnable()
        {
            YG2.onRewardAdv += OnRewardAdv;
        }

        private void OnDisable()
        {
            YG2.onRewardAdv -= OnRewardAdv;
        }

        private void OnRewardAdv(string rewardId)
        {
            Action action = _pendingRewardAction;
            _pendingRewardAction = null;

            action?.Invoke();
        }
#endif

        public void ShowInterstitialIfNeeded(int levelNumber)
        {
            if (levelNumber % _config.InterstitialEveryLevels != 0)
            {
                return;
            }

#if InterstitialAdv_yg
            if (YG2.nowAdsShow == true)
            {
                return;
            }

            YG2.InterstitialAdvShow();
#endif
        }

        public void ShowDoubleReward(Action onGranted)
        {
            ShowRewarded(_config.DoubleRewardId, onGranted);
        }

        public void ShowRewarded(string rewardId, Action onGranted)
        {
            if (onGranted == null)
            {
                return;
            }

#if RewardedAdv_yg
            if (YG2.nowAdsShow == true)
            {
                return;
            }

            _pendingRewardAction = onGranted;
            YG2.RewardedAdvShow(rewardId);
#endif
        }
    }
}
