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
        public bool IsCompleted => _targetCount <= 0;
        public int TargetCount => _targetCount;
        
        public void Decrease()
        {
            if(_targetCount>0)
            _targetCount--;
        }
    }
}
