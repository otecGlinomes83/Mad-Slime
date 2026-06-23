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

        public void Decrease()
        {
            if (TargetCount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(TargetCount), "Target Count lower than 0");
            }

            _targetCount--;
        }
    }
}