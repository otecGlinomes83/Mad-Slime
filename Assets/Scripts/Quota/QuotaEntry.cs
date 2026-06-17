using System;
using Item;
using UnityEngine;

namespace Quota
{
    [Serializable]
    public sealed class QuotaEntry
    {
        [SerializeField] private ItemDefinition _definition;
        [SerializeField] private int _targetCount = 1;

        public ItemDefinition Definition => _definition;

        public int TargetCount => _targetCount;

        public void Add(int value)
        {
            if (value <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(value), "QuotaEntry.Add value must be positive");
            }

            _targetCount += value;
        }
    }
}