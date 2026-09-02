using UnityEngine;

namespace Scriptables
{
    [CreateAssetMenu(menuName = "Mad Slime/Player Config", fileName = "NewPlayerConfig")]
    public sealed class PlayerConfig : ScriptableObject
    {
        [SerializeField] private float _baseMoveSpeed = 4f;
        [SerializeField] private float _rotationSpeed = 420f;
        [SerializeField] private float _moveSmoothTime = 0.12f;

        public float BaseMoveSpeed => _baseMoveSpeed;
        public float RotationSpeed => _rotationSpeed;
        public float MoveSmoothTime => _moveSmoothTime;
    }
}
