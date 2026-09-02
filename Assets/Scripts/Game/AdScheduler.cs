using System;
using UnityEngine;
using VContainer;

namespace Game
{
    public sealed class AdScheduler : MonoBehaviour
    {
        [SerializeField] private int _interstitialEveryLevels = 2;

        private Action _pendingRewardAction;

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
            if (levelNumber % _interstitialEveryLevels != 0)
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
