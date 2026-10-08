using System;

namespace Core
{
    public interface IAdsService
    {
        event Action RewardedOpened;

        event Action<string> RewardReceived;

        event Action RewardedClosed;

        event Action RewardedError;

        bool IsAdShowing { get; }

        bool IsPauseGame { get; }

        void ShowRewarded(string rewardId);

        void ShowInterstitial();
    }
}
