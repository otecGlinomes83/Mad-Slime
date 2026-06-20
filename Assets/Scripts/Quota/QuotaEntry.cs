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

        public QuotaEntry(ItemDefinition definition, int targetCount)
        {
            if (definition == null)
            {
                throw new ArgumentNullException(nameof(definition),
                    "QuotaEntry requires definition to be non-null.");
            }

            if (targetCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(targetCount),
                    "QuotaEntry requires targetCount to be non-negative.");
            }

            _definition = definition;
            _targetCount = targetCount;
        }

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