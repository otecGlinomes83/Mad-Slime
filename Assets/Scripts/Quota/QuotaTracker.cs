using Item;
using Quota;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public sealed class QuotaTracker : MonoBehaviour
{
    [SerializeField] private List<QuotaEntry> _quota = new List<QuotaEntry>();

    public event Action QuotaCompleted;
    public event Action<int, QuotaEntry> QuotaChanged;

    public IReadOnlyList<QuotaEntry> Entries => _quota;

    private void Awake()
    {
        for (int i = 0; i < _quota.Count; i++)
        {
            QuotaChanged?.Invoke(_quota[i].TargetCount, _quota[i]);
        }
    }

    public void DecreaseQuota(ItemDefinition definition)
    {
        for (int i = 0; i < _quota.Count; i++)
        {
            QuotaEntry entry = _quota[i];

            if (entry.Definition != definition)
            {
                continue;
            }

            _quota[i].Decrease();
            QuotaChanged?.Invoke(_quota[i].TargetCount, _quota[i]);
        }

        if (_quota.All(quotaEntry => quotaEntry.TargetCount <= 0))
        {
            QuotaCompleted?.Invoke();
        }
    }

    public bool IsQuotaItem(ItemDefinition definition)
    {
        if (definition == null)
        {
            return false;
        }

        for (int i = 0; i < _quota.Count; i++)
        {
            if (_quota[i].Definition == definition)
            {
                return true;
            }
        }

        return false;
    }
}