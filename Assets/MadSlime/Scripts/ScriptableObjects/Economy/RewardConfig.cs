using System;
using UnityEngine;

namespace Scriptables
{
    [CreateAssetMenu(menuName = "Mad Slime/Reward Config", fileName = "NewRewardConfig")]
    public class RewardConfig : ScriptableObject
    {
        [Tooltip("Базовая награда монетами за победу на уровне.")]
        [SerializeField, Min(0)] private int _baseReward = 20;

        [Tooltip("Степень роста: награда = базовая награда × номер уровня в этой степени. Без потолка.")]
        [SerializeField, Min(1f)] private float _growthExponent = 1.65f;

        [Tooltip("Минимальная доля заливки при провале, за которую хоть что-то платят. Ниже — ноль монет.")]
        [SerializeField] private float _loseMultiplierThreshold = 0.25f;

        [Tooltip("На сколько делится базовая награда при провале. 4 = четверть базовой.")]
        [SerializeField] private int _loseRewardDivisor = 4;

        public float LoseMultiplierThreshold => _loseMultiplierThreshold;
        public int LoseRewardDivisor => _loseRewardDivisor;

        public int GetReward(int level)
        {
            if (level < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(level));
            }

            if (_baseReward < 0 || _growthExponent < 1f
                || float.IsNaN(_growthExponent) || float.IsInfinity(_growthExponent))
            {
                throw new InvalidOperationException($"{name}: reward progression settings are invalid.");
            }

            double reward = Math.Round(_baseReward * Math.Pow(level, _growthExponent), MidpointRounding.AwayFromZero);
            if (reward > int.MaxValue)
            {
                throw new OverflowException("The level reward exceeds the wallet's integer range.");
            }
            return (int)reward;
        }
    }
}
