
    using System.Collections.Generic;
    using Collectables;
    using UnityEngine;

    public class PropsBank:ScriptableObject
    {
        [SerializeField] private Item[] _items;
        
        public IReadOnlyCollection<Item> Items => _items;
    }