using System;
using System.Collections.Generic;
using Skills;
using UnityEngine;

namespace Scriptables
{
    [CreateAssetMenu(menuName = "Mad Slime/Tier Table", fileName = "NewTierTable")]
    public class TierTable : ScriptableObject
    {
        [Tooltip("Строки тиров: тир предмета, его масштаб в мире и масса, которую он даёт игроку.")]
        [SerializeField] private List<TierEntry> _entries = new List<TierEntry>();

        public IReadOnlyList<TierEntry> Entries => _entries;

    }

    [Serializable]
    public class TierEntry
    {
        [Tooltip("Тир, к которому относится строка.")]
        [SerializeField] private SizeTier _tier;

        [Tooltip("Масштаб предмета этого тира при спавне.")]
        [SerializeField] private float _scale = 1f;

        [Tooltip("Масса предмета тира: сколько массы получит игрок при поглощении.")]
        [SerializeField] private int _mass = 1;

        public SizeTier Tier => _tier;
        public float Scale => _scale;
        public int Mass => _mass;
    }
}
