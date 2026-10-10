using System;
using System.Collections.Generic;
using Items;
using UnityEngine;

namespace Quota
{
    public class QuotaBoard
    {
        private List<QuotaEntry> _entries = new List<QuotaEntry>();

        public event Action<int, QuotaEntry> QuotaChanged;

        public event Action ResetCompleted;

        public IReadOnlyList<QuotaEntry> Entries => _entries;

        private int _totalTarget;

        public int TotalTarget => _totalTarget;

        public void Reset(IReadOnlyList<QuotaEntry> quota)
        {
            if (quota == null)
            {
                throw new ArgumentNullException(nameof(quota),
                    "QuotaBoard.Reset requires a quota list.");
            }

            _entries.Clear();
            _totalTarget = 0;

            for (int i = 0; i < quota.Count; i++)
            {
                _entries.Add(quota[i]);
                _totalTarget += quota[i].TargetCount;
            }

            ResetCompleted?.Invoke();
        }

        public bool IsQuotaItem(ItemDefinition definition)
        {
            QuotaEntry entry = FindEntry(definition);

            if (entry == null)
            {
                return false;
            }

            return entry.Collected < entry.TargetCount;
        }

        public bool TryRegisterCollected(ItemDefinition definition)
        {
            QuotaEntry entry = FindEntry(definition);

            if (entry == null)
            {
                return false;
            }

            if (entry.Collected >= entry.TargetCount)
            {
                return false;
            }

            entry.RegisterCollected();

            QuotaChanged?.Invoke(entry.Remaining, entry);

            return true;
        }

        private QuotaEntry FindEntry(ItemDefinition definition)
        {
            for (int i = 0; i < _entries.Count; i++)
            {
                if (_entries[i].Definition == definition)
                {
                    return _entries[i];
                }
            }

            return null;
        }
    }
}
