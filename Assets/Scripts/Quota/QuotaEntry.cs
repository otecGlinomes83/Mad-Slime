using System;
using Collectables;
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
    }
}
