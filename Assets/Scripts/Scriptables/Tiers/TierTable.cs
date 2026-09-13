using System;
using System.Collections.Generic;
using Skills;
using UnityEngine;

namespace Scriptables
{
    [CreateAssetMenu(menuName = "Mad Slime/Tier Table", fileName = "NewTierTable")]
    public sealed class TierTable : ScriptableObject
    {
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
        [SerializeField] private ItemTier _tier;
        [SerializeField] private float _scale = 1f;
        [SerializeField] private int _mass = 1;
        [SerializeField] private Color _badgeColor = Color.white;
        [SerializeField] private string _shortLabel = "S";

        public ItemTier Tier => _tier;
        public float Scale => Mathf.Max(0.01f, _scale);
        public int Mass => Mathf.Max(1, _mass);
        public Color BadgeColor => _badgeColor;
        public string ShortLabel => string.IsNullOrEmpty(_shortLabel) ? _tier.ToString() : _shortLabel;
    }
}
