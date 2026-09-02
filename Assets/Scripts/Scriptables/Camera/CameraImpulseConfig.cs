using UnityEngine;

namespace Scriptables
{
    [CreateAssetMenu(menuName = "Mad Slime/Camera Impulse Config", fileName = "NewCameraImpulseConfig")]
    public sealed class CameraImpulseConfig : ScriptableObject
    {
        [SerializeField] private AnimationCurve _massToPullStrength = AnimationCurve.Linear(0f, 0.4f, 50f, 2.5f);
        [SerializeField] private float _maxPull = 6f;
        [SerializeField] private float _tierPushStrength = 4f;
        [SerializeField] private float _maxPush = 8f;
        [SerializeField] private float _recoverSpeed = 4f;

        public AnimationCurve MassToPullStrength => _massToPullStrength;
        public float MaxPull => _maxPull;
        public float TierPushStrength => _tierPushStrength;
        public float MaxPush => _maxPush;
        public float RecoverSpeed => _recoverSpeed;
    }
}
