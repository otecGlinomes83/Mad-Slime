using System;
using System.Collections.Generic;
using Skills;
using UnityEngine;

namespace Scriptables
{
    public sealed class TierResolver : MonoBehaviour
    {
        [SerializeField] private TierScalerConfig _config;

        private readonly List<TierThreshold> _sortedByMass = new List<TierThreshold>();

        private void Awake()
        {
            if (_config == null)
            {
                throw new InvalidOperationException(
                    $"{name}: TierScalerConfig is not assigned. Drag a TierScalerConfig asset into the _config field.");
            }

            _sortedByMass.AddRange(_config.Thresholds);
            _sortedByMass.Sort((left, right) => left.RequiredMass.CompareTo(right.RequiredMass));
        }

        public ItemTier GetUnlockedTier(int mass)
        {
            ItemTier unlocked = ItemTier.Small;

            for (int i = 0; i < _sortedByMass.Count; i++)
            {
                if (mass >= _sortedByMass[i].RequiredMass)
                {
                    unlocked = _sortedByMass[i].Tier;
                }
                else
                {
                    break;
                }
            }

            return unlocked;
        }

        public float GetScaleFor(ItemTier tier)
        {
            for (int i = 0; i < _sortedByMass.Count; i++)
            {
                if (_sortedByMass[i].Tier == tier)
                {
                    return _sortedByMass[i].ScaleMultiplier;
                }
            }

            return 1f;
        }

        public float GetCameraOffsetFor(ItemTier tier)
        {
            for (int i = 0; i < _sortedByMass.Count; i++)
            {
                if (_sortedByMass[i].Tier == tier)
                {
                    return _sortedByMass[i].CameraOffsetMultiplier;
                }
            }

            return 1f;
        }
    }
}