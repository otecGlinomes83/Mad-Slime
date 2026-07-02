using System.Collections.Generic;
using UnityEngine;

namespace Scriptables
{
    [CreateAssetMenu(menuName = "Mad Slime/Tier Scaler Config", fileName = "NewTierScalerConfig")]
    public sealed class TierScalerConfig : ScriptableObject
    {
        [SerializeField] private List<TierThreshold> _thresholds = new List<TierThreshold>();

        public IReadOnlyList<TierThreshold> Thresholds => _thresholds;
    }
}