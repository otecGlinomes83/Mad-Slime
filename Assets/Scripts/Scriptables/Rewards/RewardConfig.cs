using UnityEngine;

namespace Scriptables
{
    [CreateAssetMenu(menuName = "Mad Slime/Reward Config", fileName = "NewRewardConfig")]
    public sealed class RewardConfig : ScriptableObject
    {
        [SerializeField] private int _baseReward = 50;
        [SerializeField] private float _winFullMultiplierThreshold = 1.25f;
        [SerializeField] private float _loseMultiplierThreshold = 0.25f;
        [SerializeField] private int _loseRewardDivisor = 4;

        public int BaseReward => _baseReward;
        public float WinFullMultiplierThreshold => _winFullMultiplierThreshold;
        public float LoseMultiplierThreshold => _loseMultiplierThreshold;
        public int LoseRewardDivisor => _loseRewardDivisor;
    }
}
