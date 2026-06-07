using System;
using System.Collections.Generic;
using System.Linq;
using Collectables;
using Health;
using Quota;
using UnityEngine;

public sealed class QuotaTreker : MonoBehaviour
{
    [SerializeField] private List<QuotaEntry> _quota = new List<QuotaEntry>();
    [SerializeField] private Inventory _inventory;
    [SerializeField] private Health.Health _playerHealth;
    [SerializeField] private int _minIncrease;
    [SerializeField] private int _maxIncrease;

    public IReadOnlyList<QuotaEntry> Entries => _quota;

    public event Action QuotaCompleted;
    public event Action<int, QuotaEntry> QuotaChanged;

    private readonly List<QuotaEntry> _incompleteBuffer = new List<QuotaEntry>();

    private void OnEnable()
    {
        _inventory.ItemAdded += OnItemAdded;
        _playerHealth.Damaged += OnPlayerDamaged;
    }

    private void OnDisable()
    {
        _inventory.ItemAdded -= OnItemAdded;
        _playerHealth.Damaged -= OnPlayerDamaged;
    }

    public void ReportCurrentStates()
    {
        for (int i = 0; i < _quota.Count; i++)
        {
            Recalculate(_quota[i]);
        }
    }

    private void OnItemAdded(ItemDefinition definition)
    {
        for (int i = 0; i < _quota.Count; i++)
        {
            QuotaEntry entry = _quota[i];

            if (entry.Definition != definition)
            {
                continue;
            }

            Recalculate(entry);
        }

        if (IsAllQuotaComplete())
        {
            QuotaCompleted?.Invoke();
            Debug.Log($"All quota collected");
        }
    }

    private void OnPlayerDamaged()
    {
        _incompleteBuffer.Clear();
        for (int i = 0; i < _quota.Count; i++)
        {
            QuotaEntry entry = _quota[i];
            if (IsComplete(entry) == false)
            {
                _incompleteBuffer.Add(entry);
            }
        }

        if (_incompleteBuffer.Count == 0)
        {
            return;
        }

        int amount = UnityEngine.Random.Range(_minIncrease, _maxIncrease + 1);
        if (amount <= 0)
        {
            return;
        }

        int randomIndex = UnityEngine.Random.Range(0, _incompleteBuffer.Count);
        QuotaEntry target = _incompleteBuffer[randomIndex];

        target.Add(amount);
        Recalculate(target);
    }

    private void Recalculate(QuotaEntry entry)
    {
        int collected;
        if (_inventory.IsContains(entry.Definition))
        {
            collected = _inventory.GetCount(entry.Definition);
        }
        else
        {
            collected = 0;
        }

        int remaining = Mathf.Max(0, entry.TargetCount - collected);
        QuotaChanged?.Invoke(remaining, entry);
    }

    private bool IsComplete(QuotaEntry entry)
    {
        if (_inventory.IsContains(entry.Definition) == false)
        {
            return false;
        }

        return _inventory.GetCount(entry.Definition) >= entry.TargetCount;
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
