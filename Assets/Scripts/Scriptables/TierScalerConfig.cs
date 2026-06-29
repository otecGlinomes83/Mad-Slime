using Skills;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Scriptables
{
    [CreateAssetMenu(menuName = "Mad Slime/Tier Scaler Config", fileName = "NewTierScalerConfig")]
    public sealed class TierScalerConfig : ScriptableObject
    {
        [Serializable]
        public sealed class TierThreshold
        {
            [SerializeField] private ItemTier _tier;
            [SerializeField] private int _requiredMass;
            [SerializeField] private float _scaleMultiplier = 1f;

            public ItemTier Tier => _tier;
            public int RequiredMass => _requiredMass;
            public float ScaleMultiplier => _scaleMultiplier;
        }

        [SerializeField] private List<TierThreshold> _thresholds = new List<TierThreshold>();

        public IReadOnlyList<TierThreshold> Thresholds => _thresholds;

        public ItemTier GetUnlockedTier(int mass)
        {
            ItemTier unlocked = ItemTier.Small;

            for (int i = 0; i < _thresholds.Count; i++)
            {
                TierThreshold threshold = _thresholds[i];

                if (mass >= threshold.RequiredMass)
                {
                    unlocked = threshold.Tier;
                }
            }

            return unlocked;
        }

        public float GetScaleFor(ItemTier tier)
        {
            for (int i = 0; i < _thresholds.Count; i++)
            {
                TierThreshold threshold = _thresholds[i];

                if (threshold.Tier == tier)
                {
                    return threshold.ScaleMultiplier;
                }
            }

            return 1f;
        }
    }
}