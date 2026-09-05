using Skills;
using System;
using UnityEngine;

namespace Scriptables
{
    [Serializable]
    public sealed class TierThreshold
    {
        [SerializeField] private ItemTier _tier;
        [SerializeField] private int _requiredMass;
        [SerializeField] private float _scaleMultiplier = 1f;
        [SerializeField] private float _speed = 1f;
        [SerializeField] private float _cameraOffsetMultiplier = 1f;
        [SerializeField] private string _label;

        public ItemTier Tier => _tier;
        public int RequiredMass => _requiredMass;
        public float ScaleMultiplier => _scaleMultiplier;
        public float CameraOffsetMultiplier => _cameraOffsetMultiplier;
        public float Speed => _speed;
        public string Label => _label;
    }
}
