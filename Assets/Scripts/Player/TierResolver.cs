using Scriptables;
using System;
using System.Collections.Generic;
using Skills;
using UnityEngine;

namespace Player
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

        public float GetSpeedFor(ItemTier tier)
        {
            for (int i = 0; i < _sortedByMass.Count; i++)
            {
                if (_sortedByMass[i].Tier == tier)
                {
                    return _sortedByMass[i].Speed;
                }
            }

            if (_sortedByMass.Count > 0)
            {
                return _sortedByMass[0].Speed;
            }

            return 4f;
        }

        public string GetTierLabelFor(ItemTier tier)
        {
            for (int i = 0; i < _sortedByMass.Count; i++)
            {
                if (_sortedByMass[i].Tier == tier)
                {
                    string label = _sortedByMass[i].Label;

                    if (string.IsNullOrEmpty(label) == false)
                    {
                        return label;
                    }

                    return tier.ToString();
                }
            }

            return tier.ToString();
        }

        public float GetTierProgress(int mass)
        {
            float previousThresholdMass = 0f;
            float nextThresholdMass = -1f;

            for (int i = 0; i < _sortedByMass.Count; i++)
            {
                int thresholdMass = _sortedByMass[i].RequiredMass;

                if (thresholdMass <= mass)
                {
                    previousThresholdMass = thresholdMass;
                }
                else
                {
                    nextThresholdMass = thresholdMass;
                    break;
                }
            }

            if (nextThresholdMass < 0f)
            {
                return 1f;
            }

            float segment = nextThresholdMass - previousThresholdMass;

            if (segment <= 0f)
            {
                return 1f;
            }

            return Mathf.Clamp01((mass - previousThresholdMass) / segment);
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