using System;
using System.Collections.Generic;
using Items;
using UnityEngine;

namespace Scriptables
{
    [CreateAssetMenu(menuName = "Mad Slime/Prop Set", fileName = "NewPropSet")]
    public sealed class PropSet : ScriptableObject
    {
        [SerializeField] private List<Item> _props = new List<Item>();
        [SerializeField] private List<PropVariant> _variants = new List<PropVariant>();

        public IReadOnlyList<Item> Props => _props;
        public IReadOnlyList<PropVariant> Variants => _variants;
    }

    [Serializable]
    public sealed class PropVariant
    {
        [SerializeField] private Item _prefab;
        [SerializeField] private ItemDefinition _definition;

        public PropVariant()
        {
        }

        public PropVariant(Item prefab, ItemDefinition definition)
        {
            _prefab = prefab;
            _definition = definition;
        }

        public Item Prefab => _prefab;
        public ItemDefinition Definition => _definition;
    }
}
