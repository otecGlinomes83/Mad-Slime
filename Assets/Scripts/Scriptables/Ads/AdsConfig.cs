using UnityEngine;

namespace Scriptables
{
    [CreateAssetMenu(menuName = "Mad Slime/Ads Config", fileName = "NewAdsConfig")]
    public sealed class AdsConfig : ScriptableObject
    {
        [SerializeField] private int _interstitialEveryLevels = 2;
        [SerializeField] private string _doubleRewardId = "DoubleReward";

        public int InterstitialEveryLevels => _interstitialEveryLevels;
        public string DoubleRewardId => _doubleRewardId;
    }
}
