using UnityEngine;

namespace Scriptables
{
    [CreateAssetMenu(menuName = "Mad Slime/Yandex Config", fileName = "NewYandexConfig")]
    public sealed class YandexConfig : ScriptableObject
    {
        [Header("Ads")]
        [Tooltip("ID rewarded-блока удвоения награды. Должен совпадать с блоком, созданным в Яндекс.Консоли.")]
        [SerializeField] private string _doubleRewardId = "DoubleReward";

        [Tooltip("ID rewarded-блока «добить заливку» после провала. Должен совпадать с блоком в Яндекс.Консоли.")]
        [SerializeField] private string _fillRescueRewardId = "NextLevel";

        [Tooltip("ID rewarded-блока крутки рулетки. Должен совпадать с блоком в Яндекс.Консоли.")]
        [SerializeField] private string _rouletteRewardId = "Roulette";

        [Header("Leaderboard")]
        [Tooltip("Технический id лидерборда в Яндекс.Консоли (не отображаемое имя).")]
        [SerializeField] private string _leaderboardName = "max_level";

        public string DoubleRewardId => _doubleRewardId;
        public string FillRescueRewardId => _fillRescueRewardId;
        public string RouletteRewardId => _rouletteRewardId;
        public string LeaderboardName => _leaderboardName;
    }
}
