using System.Collections.Generic;
using UnityEngine;

namespace Scriptables
{
    [CreateAssetMenu(menuName = "Mad Slime/Player Config", fileName = "NewPlayerConfig")]
    public sealed class PlayerConfig : ScriptableObject
    {
        [Header("Movement")]
        [SerializeField, Min(0.1f)] private float _baseMoveSpeed = 4f;
        [SerializeField, Min(1f)] private float _rotationSpeed = 420f;
        [SerializeField, Min(0.01f)] private float _moveSmoothTime = 0.12f;

        [Header("Absorption")]
        [SerializeField, Min(0.05f)] private float _absorptionDuration = 0.3f;

        [Header("Growth")]
        [SerializeField, Min(0)] private int _startMass;
        [SerializeField] private List<PlayerTierThreshold> _thresholds = new List<PlayerTierThreshold>();

        public float BaseMoveSpeed => _baseMoveSpeed;
        public float RotationSpeed => _rotationSpeed;
        public float MoveSmoothTime => _moveSmoothTime;
        public float AbsorptionDuration => _absorptionDuration;
        public int StartMass => _startMass;
        public IReadOnlyList<PlayerTierThreshold> Thresholds => _thresholds;
    }
}
