using System;
using Collectables;
using UnityEngine;

namespace Quota
{
    public sealed class QuotaTracker : MonoBehaviour
    {
        [SerializeField] private Inventory _inventory;
        [SerializeField] private SessionRules _rules;

        private int _quotaCollected;
        private int _otherCollected;

        public int QuotaCollected => _quotaCollected;
        public int OtherCollected => _otherCollected;

        public event Action ProgressChanged;

        private void OnEnable()
        {
            _inventory.ItemAdded += OnItemAdded;

            foreach (QuotaEntry entry in _rules.Quota)
            {
                int neededCount = entry.TargetCount;
                Debug.Log($"Need {neededCount} more of {entry.Definition.name}");
            }
        }

        private void OnDisable()
        {
            _inventory.ItemAdded -= OnItemAdded;
        }

        public int GetQuotaTargetCount(ItemDefinition definition)
        {
            for (int i = 0; i < _rules.Quota.Count; i++)
            {
                if (_rules.Quota[i].Definition == definition)
                {
                    return _rules.Quota[i].TargetCount;
                }
            }

            return 0;
        }

        public bool IsQuotaCompleted()
        {
            for (int i = 0; i < _rules.Quota.Count; i++)
            {
                QuotaEntry entry = _rules.Quota[i];

                if (_inventory.GetCount(entry.Definition) < entry.TargetCount)
                {
                    return false;
                }
            }

            return true;
        }

        private void OnItemAdded(ItemDefinition definition)
        {
            if (IsQuotaItem(definition) == true)
            {
                if (IsEnoughQuotaCollected(definition)==false)
                {
                    _quotaCollected++;
                    Debug.Log($"Quota collected: {_quotaCollected}");
                }
            }
            else
            {
                _otherCollected++;
                Debug.Log($"Other collected: {_otherCollected}");
            }

            ShowQuotaProgress();

            ProgressChanged?.Invoke();
        }

        private bool IsEnoughQuotaCollected(ItemDefinition definition)
        {
            for (int i = 0; i < _rules.Quota.Count; i++)
            {
                if (_rules.Quota[i].Definition == definition)
                {
                    return _inventory.GetCount(definition) >= _rules.Quota[i].TargetCount;
                }
            }
            
            return false;
        }

        private void ShowQuotaProgress()
        {
            foreach (QuotaEntry entry in _rules.Quota)
            {
                int neededCount = entry.TargetCount - _inventory.GetCount(entry.Definition);
                Debug.Log($"Need {neededCount} more of {entry.Definition.name}");
            }
        }


        private bool IsQuotaItem(ItemDefinition definition)
        {
            for (int i = 0; i < _rules.Quota.Count; i++)
            {
                if (_rules.Quota[i].Definition == definition)
                {
                    return true;
                }
            }

            return false;
        }
    }
}