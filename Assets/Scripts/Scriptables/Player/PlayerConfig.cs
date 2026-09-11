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
        [SerializeField, Min(1)] private int _massPickupDivisor = 4;
        [SerializeField, Min(0.05f)] private float _absorptionDuration = 0.3f;

        public float BaseMoveSpeed => _baseMoveSpeed;
        public float RotationSpeed => _rotationSpeed;
        public float MoveSmoothTime => _moveSmoothTime;
        public int MassPickupDivisor => Mathf.Max(1, _massPickupDivisor);
        public float AbsorptionDuration => _absorptionDuration;
    }
}
