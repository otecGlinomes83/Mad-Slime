using UnityEngine;

namespace Scriptables
{
    [CreateAssetMenu(menuName = "Mad Slime/Yandex Config", fileName = "NewYandexConfig")]
    public sealed class YandexConfig : ScriptableObject
    {
        [Header("Ads")]
        [Tooltip("Раз в сколько уровней показывать межстраничную рекламу.")]
        [SerializeField, Min(1)] private int _interstitialEveryLevels = 2;

        [Tooltip("ID rewarded-блока удвоения награды. Должен совпадать с блоком, созданным в Яндекс.Консоли.")]
        [SerializeField] private string _doubleRewardId = "DoubleReward";

        [Tooltip("ID rewarded-блока «следующий уровень» после провала.")]
        [SerializeField] private string _nextLevelRewardId = "NextLevel";

        [Header("Leaderboard")]
        [Tooltip("Технический id лидерборда в Яндекс.Консоли (не отображаемое имя).")]
        [SerializeField] private string _leaderboardName = "max_level";

        public int InterstitialEveryLevels => Mathf.Max(1, _interstitialEveryLevels);
        public string DoubleRewardId => _doubleRewardId;
        public string NextLevelRewardId => _nextLevelRewardId;
        public string LeaderboardName => _leaderboardName;
    }
}
