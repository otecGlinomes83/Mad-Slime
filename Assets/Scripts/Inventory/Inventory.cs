using System;
using System.Collections.Generic;
using Collectables;
using UnityEngine;

public sealed class Inventory : MonoBehaviour
{
    private readonly Dictionary<ItemDefinition, int> _items = new Dictionary<ItemDefinition, int>();

    public IReadOnlyDictionary<ItemDefinition, int> Items => _items;

    public event Action<ItemDefinition> ItemAdded;

    public void Add(ItemDefinition definition)
    {
        if (definition == null)
        {
            return;
        }

        if (_items.TryGetValue(definition, out int currentCount) == true)
        {
            _items[definition] = currentCount + 1;
        }
        else
        {
            _items.Add(definition, 1);
        }

        ItemAdded?.Invoke(definition);
    }

    public int GetCount(ItemDefinition definition)
    {
        if (definition == null)
        {
            return 0;
        }

        if (_items.TryGetValue(definition, out int count) == true)
        {
            return count;
        }

        return 0;
    }
}
