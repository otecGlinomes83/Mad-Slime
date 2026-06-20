using System.Collections.Generic;
using Item;
using UnityEngine;

public sealed class FillCounter : MonoBehaviour
{
    [SerializeField] private Inventory _inventory;
    [SerializeField] private QuotaTracker _quotaTracker;

    private int _quotaCount;
    private int _simpleCount;

    public int Overage { get; private set; }
    public int LastCounted { get; private set; }

    public int Calculate(int targetCount)
    {
        _quotaCount = 0;
        _simpleCount = 0;

        foreach (KeyValuePair<ItemDefinition, int> kvp in _inventory.Items)
        {
            if (_quotaTracker.IsQuotaItem(kvp.Key))
            {
                _quotaCount += kvp.Value;
            }
            else
            {
                _simpleCount += kvp.Value;
            }
        }

        int converted = _quotaCount + _simpleCount / 2;
        int cubesForFill = Mathf.Min(converted, targetCount);
        Overage = Mathf.Max(0, converted - targetCount);
        LastCounted = cubesForFill;

        return cubesForFill;
    }
}
