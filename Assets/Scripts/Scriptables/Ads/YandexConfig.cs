using UnityEngine;

namespace Scriptables
{
    [CreateAssetMenu(menuName = "Mad Slime/Yandex Config", fileName = "NewYandexConfig")]
    public sealed class YandexConfig : ScriptableObject
    {
        [Header("Ads")]
        [SerializeField, Min(1)] private int _interstitialEveryLevels = 2;
        [SerializeField] private string _doubleRewardId = "DoubleReward";
        [SerializeField] private string _nextLevelRewardId = "NextLevel";

        [Header("Leaderboard")]
        [SerializeField] private string _leaderboardName = "max_level";

        public int InterstitialEveryLevels => Mathf.Max(1, _interstitialEveryLevels);
        public string DoubleRewardId => _doubleRewardId;
        public string NextLevelRewardId => _nextLevelRewardId;
        public string LeaderboardName => _leaderboardName;
    }
}
