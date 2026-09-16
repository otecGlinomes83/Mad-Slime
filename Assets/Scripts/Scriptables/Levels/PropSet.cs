using System;
using System.Collections.Generic;
using Items;
using UnityEngine;

namespace Scriptables
{
    [CreateAssetMenu(menuName = "Mad Slime/Prop Set", fileName = "NewPropSet")]
    public sealed class PropSet : ScriptableObject
    {
        [Tooltip("Базовые предметы-пропсы уровня.")]
        [SerializeField] private List<Item> _props = new List<Item>();

        [Tooltip("Варианты пропсов: тот же префаб с другим определением (иконка и тир).")]
        [SerializeField] private List<PropVariant> _variants = new List<PropVariant>();

        public IReadOnlyList<Item> Props => _props;
        public IReadOnlyList<PropVariant> Variants => _variants;
    }

    [Serializable]
    public sealed class PropVariant
    {
        [Tooltip("Префаб предмета.")]
        [SerializeField] private Item _prefab;

        [Tooltip("Определение, присваиваемое варианту: иконка и тир.")]
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
