using System;
using UnityEngine;
using YG;
#if UNITY_WEBGL && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif

namespace Game
{
    public sealed class YandexAdsBridge : MonoBehaviour
    {
        public const string ReceiverName = "YandexAdsBridge";

        public event Action<string> RewardedOpened;
        public event Action<string> RewardedReceived;
        public event Action<string> RewardedClosed;
        public event Action<string> RewardedError;
        public event Action InterstitialOpened;
        public event Action InterstitialClosed;
        public event Action InterstitialError;
        public event Action<string> ScoreSet;
        public event Action<string> ScoreError;
        public event Action<string> EntriesReceived;
        public event Action<string> EntriesError;

#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern void RewardedAdvShowMadSlime_js(string id);

        [DllImport("__Internal")]
        private static extern void InterstitialAdvShowMadSlime_js();

        [DllImport("__Internal")]
        private static extern void SetLeaderboardScoreMadSlime_js(string name, int score, string extraParam);

        [DllImport("__Internal")]
        private static extern void GetLeaderboardEntriesMadSlime_js(string name, int quantityTop);
#endif

        public static YandexAdsBridge Create()
        {
            GameObject bridgeObject = new GameObject(ReceiverName);
            return bridgeObject.AddComponent<YandexAdsBridge>();
        }

        public void ShowRewarded(string rewardId)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            RewardedAdvShowMadSlime_js(rewardId);
#else
            Debug.Log($"[Ads] editor simulation: rewarded '{rewardId}' granted");
            OnRewardedOpen(rewardId);
            OnRewardedReceived(rewardId);
            OnRewardedClosed(rewardId);
#endif
        }

        public void ShowInterstitial()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            InterstitialAdvShowMadSlime_js();
#else
            Debug.Log("[Ads] editor simulation: interstitial shown");
            OnInterstitialClosed(string.Empty);
#endif
        }

        public void SetLeaderboardScore(string leaderboardName, int score, string extraParam)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            SetLeaderboardScoreMadSlime_js(leaderboardName, score, extraParam);
#else
            Debug.Log($"[Ads] editor simulation: leaderboard '{leaderboardName}' <- {score} ({extraParam})");
            OnScoreSet(leaderboardName);
#endif
        }

        public void GetLeaderboardEntries(string leaderboardName, int quantityTop)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            GetLeaderboardEntriesMadSlime_js(leaderboardName, quantityTop);
#else
            Debug.Log($"[Ads] editor simulation: leaderboard '{leaderboardName}' top {quantityTop}");
            LeaderboardPayload payload = new LeaderboardPayload
            {
                userRank = 1,
                entries = new LeaderboardEntry[]
                {
                    new LeaderboardEntry { rank = 1, score = 100, name = "Player" }
                }
            };
            OnEntriesReceived(JsonUtility.ToJson(payload));
#endif
        }

        private void OnEntriesReceived(string jsonPayload)
        {
            EntriesReceived?.Invoke(jsonPayload);
        }

        private void OnEntriesError(string leaderboardName)
        {
            EntriesError?.Invoke(leaderboardName);
        }

        [Serializable]
        public sealed class LeaderboardPayload
        {
            public int userRank;
            public LeaderboardEntry[] entries;
        }

        [Serializable]
        public sealed class LeaderboardEntry
        {
            public int rank;
            public int score;
            public string name;
            public string extra;
        }

        private void OnRewardedOpen(string rewardId)
        {
            YG2.nowRewardAdv = true;
            RewardedOpened?.Invoke(rewardId);
        }

        private void OnRewardedReceived(string rewardId)
        {
            RewardedReceived?.Invoke(rewardId);
        }

        private void OnRewardedClosed(string rewardId)
        {
            YG2.nowRewardAdv = false;
            RewardedClosed?.Invoke(rewardId);
        }

        private void OnRewardedError(string rewardId)
        {
            YG2.nowRewardAdv = false;
            RewardedError?.Invoke(rewardId);
        }

        private void OnInterstitialOpened(string payload)
        {
            YG2.nowInterAdv = true;
            InterstitialOpened?.Invoke();
        }

        private void OnInterstitialClosed(string payload)
        {
            YG2.nowInterAdv = false;
            InterstitialClosed?.Invoke();
        }

        private void OnInterstitialError(string payload)
        {
            YG2.nowInterAdv = false;
            InterstitialError?.Invoke();
        }

        private void OnScoreSet(string leaderboardName)
        {
            ScoreSet?.Invoke(leaderboardName);
        }

        private void OnScoreError(string leaderboardName)
        {
            ScoreError?.Invoke(leaderboardName);
        }
    }
}
