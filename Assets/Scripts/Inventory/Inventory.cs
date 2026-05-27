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
            throw new ArgumentNullException(nameof(definition), "Inventory.Add was called with a null ItemDefinition.");
        }

        if (_items.TryGetValue(definition, out int currentCount))
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
            throw new ArgumentNullException(nameof(definition), "Inventory.GetCount was called with a null ItemDefinition.");
        }

        if (_items.TryGetValue(definition, out int count))
        {
            return count;
        }

        throw new InvalidOperationException(string.Format("Inventory does not contain item '{0}'. Call Add before GetCount or check presence via Items.", definition.DisplayName));
    }
}