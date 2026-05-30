using System;
using System.Collections.Generic;
using System.Linq;
using Collectables;
using Quota;
using UnityEngine;

public sealed class QuotaTreker : MonoBehaviour
{
    [SerializeField] private List<QuotaEntry> _quota = new List<QuotaEntry>();
    [SerializeField] private Inventory _inventory;

    public event Action QuotaCompleted;
    public event Action<int, QuotaEntry> QuotaChanged;

    private void OnEnable()
    {
        _inventory.ItemAdded += OnItemAdded;
    }

    private void OnDisable()
    {
        _inventory.ItemAdded -= OnItemAdded;
    }

    private void OnItemAdded(ItemDefinition definition)
    {
        if (IsAllQuotaComplete())
        {
            QuotaCompleted?.Invoke();
            Debug.Log($"All quota collected");
            return;
        }

        foreach (QuotaEntry entry in _quota)
        {
            if (entry.Definition != definition)
            {
                continue;
            }

            int remaining = entry.TargetCount - _inventory.GetCount(definition);

            if (remaining <= 0)
            {
                continue;
            }

            Debug.Log($"{entry.Definition.DisplayName}/{remaining}");
            QuotaChanged?.Invoke(remaining, entry);
        }
    }

    private bool IsQuotaComplete(QuotaEntry entry)
    {
        int collectedQuota = _inventory.GetCount(entry.Definition);

        return collectedQuota >= entry.TargetCount;
    }

    private bool IsAllQuotaComplete()
    {
        for (int i = 0; i < _quota.Count; i++)
        {
            QuotaEntry quotaEntry = _quota[i];

            if (_inventory.IsContains(quotaEntry.Definition) == false)
            {
                return false;
            }

            if (_inventory.GetCount(quotaEntry.Definition) < quotaEntry.TargetCount)
            {
                return false;
            }
        }

        return true;
    }
}