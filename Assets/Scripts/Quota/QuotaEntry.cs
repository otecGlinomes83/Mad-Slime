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

        private int _collected;

        public ItemDefinition Definition => _definition;
        public int TargetCount => _targetCount;
        public int Collected => _collected;
        public int Remaining => Mathf.Max(0, _targetCount - _collected);

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

        public void RegisterCollected()
        {
            _collected++;
        }
    }
}
