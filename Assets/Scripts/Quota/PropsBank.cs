
    using System.Collections.Generic;
    using Item;
    using UnityEngine;

    public class PropsBank:ScriptableObject
    {
        [SerializeField] private Item.Item[] _items;
        
        public IReadOnlyCollection<Item.Item> Items => _items;
    }