using UnityEngine;

namespace Scriptables
{
    [CreateAssetMenu(menuName = "Mad Slime/Reward Config", fileName = "NewRewardConfig")]
    public sealed class RewardConfig : ScriptableObject
    {
        [Tooltip("Базовая награда монетами за победу на уровне.")]
        [SerializeField] private int _baseReward = 50;

        [Tooltip("Минимальная доля заливки при провале, за которую хоть что-то платят. Ниже — ноль монет.")]
        [SerializeField] private float _loseMultiplierThreshold = 0.25f;

        [Tooltip("На сколько делится базовая награда при провале. 4 = четверть базовой.")]
        [SerializeField] private int _loseRewardDivisor = 4;

        public int BaseReward => _baseReward;
        public float LoseMultiplierThreshold => _loseMultiplierThreshold;
        public int LoseRewardDivisor => _loseRewardDivisor;
    }
}
