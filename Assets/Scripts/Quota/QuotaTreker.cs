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
        if (_quota.All(entry => entry.IsCompleted == true))
        {
            QuotaCompleted?.Invoke();
            Debug.Log($"All quota collected");
            return;
        }

        foreach (QuotaEntry entry in _quota)
        {
            if (entry.Definition == definition)
            {
                if (entry.IsCompleted == false)
                {
                    entry.Decrease();
                    int remaining = entry.TargetCount - _inventory.GetCount(definition);

                    Debug.Log($"{entry.Definition.DisplayName}/{remaining}");
                    QuotaChanged?.Invoke(remaining, entry);
                    return;
                }
            }
        }
    }
}