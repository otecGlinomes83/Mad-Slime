using System;
using System.Collections.Generic;
using Skills;
using UnityEngine;

namespace Scriptables
{
    [CreateAssetMenu(menuName = "Mad Slime/Tier Table", fileName = "NewTierTable")]
    public sealed class TierTable : ScriptableObject
    {
        [Tooltip("Строки тиров: тир предмета, его масштаб в мире и масса, которую он даёт игроку.")]
        [SerializeField] private List<TierEntry> _entries = new List<TierEntry>();

        public IReadOnlyList<TierEntry> Entries => _entries;

        public TierEntry Get(ItemTier tier)
        {
            for (int i = 0; i < _entries.Count; i++)
            {
                if (_entries[i].Tier == tier)
                {
                    return _entries[i];
                }
            }

            throw new InvalidOperationException(
                $"TierTable '{name}': no entry for tier '{tier}'. Add a row for it.");
        }
    }

    [Serializable]
    public sealed class TierEntry
    {
        [Tooltip("Тир, к которому относится строка.")]
        [SerializeField] private ItemTier _tier;

        [Tooltip("Масштаб предмета этого тира при спавне.")]
        [SerializeField] private float _scale = 1f;

        [Tooltip("Масса предмета тира: сколько массы получит игрок при поглощении.")]
        [SerializeField] private int _mass = 1;

        public ItemTier Tier => _tier;
        public float Scale => Mathf.Max(0.01f, _scale);
        public int Mass => Mathf.Max(1, _mass);
    }
}
