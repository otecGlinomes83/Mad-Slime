using Item;
using Quota;
using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class QuotaTracker : MonoBehaviour
{
    private readonly List<QuotaEntry> _quota = new List<QuotaEntry>();
    [SerializeField] private Inventory _inventory;
    [SerializeField] private Health.Health _playerHealth;
    [SerializeField] private int _minIncrease;
    [SerializeField] private int _maxIncrease;
    [SerializeField] private int _minDeathIncrease = 5;
    [SerializeField] private int _maxDeathIncrease = 10;

    public IReadOnlyList<QuotaEntry> Entries => _quota;

    public event Action QuotaCompleted;
    public event Action<int, QuotaEntry> QuotaChanged;

    private readonly List<QuotaEntry> _incompleteBuffer = new List<QuotaEntry>();

    private void OnEnable()
    {
        _inventory.ItemAdded += OnItemAdded;
        _inventory.Cleared += ReportCurrentStates;
        _playerHealth.Damaged += OnPlayerDamaged;
        _playerHealth.Died += OnPlayerDie;
    }

    private void OnDisable()
    {
        _inventory.ItemAdded -= OnItemAdded;
        _inventory.Cleared -= ReportCurrentStates;
        _playerHealth.Damaged -= OnPlayerDamaged;
        _playerHealth.Died -= OnPlayerDie;
    }

    public void ReportCurrentStates()
    {
        for (int i = 0; i < _quota.Count; i++)
        {
            Recalculate(_quota[i]);
        }
    }

    public void Initialize(List<QuotaEntry> entries)
    {
        if (entries == null)
        {
            throw new ArgumentNullException(nameof(entries),
                "QuotaTracker.Initialize requires entries to be non-null.");
        }

        _quota.Clear();
        _quota.AddRange(entries);
        ReportCurrentStates();
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
        }
    }

    private void OnPlayerDie()
    {
        int amount = UnityEngine.Random.Range(_minDeathIncrease, _maxDeathIncrease + 1);
        if (amount <= 0)
        {
            return;
        }

        int randomIndex = UnityEngine.Random.Range(0, _quota.Count);
        QuotaEntry target = _quota[randomIndex];

        target.Add(amount);
        ReportCurrentStates();
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