
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Mad Slime/Props Bank", fileName = "NewPropsBank")]
public class PropsBank:ScriptableObject
    {
        [SerializeField] private Item.Item[] _items;

        public IReadOnlyList<Item.Item> Items => _items;
    }