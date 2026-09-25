using System;
using Core;
using YG;

namespace Adapters
{
    public sealed class Yg2AdsService : IAdsService
    {
        public event Action RewardedOpened;

        public event Action<string> RewardReceived;

        public event Action RewardedClosed;

        public event Action RewardedError;

        public bool IsAdShowing => YG2.nowAdsShow;

        public bool IsPauseGame => YG2.isPauseGame;

        public Yg2AdsService()
        {
            YG2.onOpenRewardedAdv += OnRewardedOpened;
            YG2.onRewardAdv += OnRewardReceived;
            YG2.onCloseRewardedAdv += OnRewardedClosed;
            YG2.onErrorRewardedAdv += OnRewardedError;
        }

        public void ShowRewarded(string rewardId)
        {
            YG2.RewardedAdvShow(rewardId);
        }

        public void ShowInterstitial()
        {
            YG2.InterstitialAdvShow();
        }

        private void OnRewardedOpened()
        {
            RewardedOpened?.Invoke();
        }

        private void OnRewardReceived(string rewardId)
        {
            RewardReceived?.Invoke(rewardId);
        }

        private void OnRewardedClosed()
        {
            RewardedClosed?.Invoke();
        }

        private void OnRewardedError()
        {
            RewardedError?.Invoke();
        }
    }
}
