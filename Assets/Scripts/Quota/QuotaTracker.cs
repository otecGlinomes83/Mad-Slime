using Item;
using Quota;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using YG;

public sealed class QuotaTracker : MonoBehaviour
{
    [SerializeField] private List<QuotaEntry> _quota = new List<QuotaEntry>();

    public event Action QuotaCompleted;
    public event Action<int, QuotaEntry> QuotaChanged;

    private void Awake()
    {
        for (int i = 0; i < _quota.Count; i++)
        {
            YG2.saves.TargetQuotaCount += _quota[i].TargetCount;
        }
    }

    private void Start()
    {
        for (int i = 0; i < _quota.Count; i++)
        {
            QuotaChanged?.Invoke(_quota[i].Remaining, _quota[i]);
        }
    }

    public void RegisterCollected(ItemDefinition definition)
    {
        if (definition == null)
        {
            return;
        }

        int entryIndex = FindEntryIndex(definition);

        if (entryIndex < 0)
        {
            return;
        }

        QuotaEntry entry = _quota[entryIndex];

        entry.RegisterCollected();

        QuotaChanged?.Invoke(entry.Remaining, entry);

        if (_quota.All(entry => entry.Collected >= entry.TargetCount))
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

        int entryIndex = FindEntryIndex(definition);

        if (entryIndex < 0)
        {
            return false;
        }

        QuotaEntry entry = _quota[entryIndex];
        return entry.Collected < entry.TargetCount;
    }

    private int FindEntryIndex(ItemDefinition definition)
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
}
