using System;
using System.Collections.Generic;
using Items;
using Quota;
using UnityEngine;

namespace Game
{
    public sealed class LevelProgress
    {
        private readonly List<QuotaEntry> _quota = new List<QuotaEntry>();

        private int _collectedQuotaCount;
        private int _collectedDefaultCount;
        private int _totalQuotaTarget;
        private bool _isQuotaCompleted;

        public event Action<ItemDefinition> ItemCollected;
        public event Action<int, QuotaEntry> QuotaChanged;
        public event Action QuotaCompleted;

        public IReadOnlyList<QuotaEntry> Quota => _quota;
        public int TotalQuotaTarget => _totalQuotaTarget;
        public int CollectedQuotaCount => _collectedQuotaCount;

        public float FillPercent
        {
            get
            {
                if (_totalQuotaTarget <= 0)
                {
                    return 0f;
                }

                float percent = (float)(_collectedQuotaCount + _collectedDefaultCount) / _totalQuotaTarget;

                return Mathf.Clamp01(percent);
            }
        }

        public void Reset(IReadOnlyList<QuotaEntry> quota)
        {
            _quota.Clear();
            _collectedQuotaCount = 0;
            _collectedDefaultCount = 0;
            _isQuotaCompleted = false;
            _totalQuotaTarget = 0;

            for (int i = 0; i < quota.Count; i++)
            {
                _quota.Add(quota[i]);
                _totalQuotaTarget += quota[i].TargetCount;
            }
        }

        public void RegisterCollected(ItemDefinition definition)
        {
            if (definition == null)
            {
                return;
            }

            ItemCollected?.Invoke(definition);

            int entryIndex = FindQuotaIndex(definition);

            if (entryIndex >= 0)
            {
                QuotaEntry entry = _quota[entryIndex];

                if (entry.Collected < entry.TargetCount)
                {
                    entry.RegisterCollected();
                    _collectedQuotaCount++;

                    QuotaChanged?.Invoke(entry.Remaining, entry);

                    CheckCompletion();
                    return;
                }
            }

            _collectedDefaultCount++;
        }

        private int FindQuotaIndex(ItemDefinition definition)
        {
            for (int i = 0; i < _quota.Count; i++)
            {
                if (_quota[i].Definition == definition)
                {
                    return i;
                }
            }

            return -1;
        }

        private void CheckCompletion()
        {
            if (_isQuotaCompleted == true)
            {
                return;
            }

            if (_collectedQuotaCount < _totalQuotaTarget)
            {
                return;
            }

            _isQuotaCompleted = true;
            QuotaCompleted?.Invoke();
        }
    }
}
