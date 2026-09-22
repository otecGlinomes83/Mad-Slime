using Player;
using Skills;
using System;
using UnityEngine;
using VContainer;

namespace CameraSystem
{
    public sealed class CameraFollow : MonoBehaviour
    {
        [SerializeField] private Transform _target;
        [SerializeField] private float _positionSmoothTime = 0.25f;
        [SerializeField] private float _maxPositionSpeed = 50f;
        [SerializeField, Min(0.1f)] private float _shakeFrequency = 25f;
        [SerializeField, Range(0f, 1f)] private Vector2 _shakeAxes = new Vector2(1f, 1f);

        private TierResolver _tierResolver;
        private PlayerTier _playerTier;
        private CameraImpulse _impulse;

        private Vector3 _positionVelocity;
        private Vector3 _currentOffset;

        private Vector3 _startOffset;

        private Camera _camera;
        private float _baseFov;

        [Inject]
        public void Construct(TierResolver tierResolver, PlayerTier playerTier, CameraImpulse impulse)
        {
            _tierResolver = tierResolver;
            _playerTier = playerTier;
            _impulse = impulse;
        }

        private void Awake()
        {
            if (_target == null)
            {
                throw new InvalidOperationException(
                    $"{name}: Target is not assigned. Drag a Transform into the _target field in the inspector.");
            }

            if (_tierResolver == null)
            {
                throw new InvalidOperationException(
                    $"{name}: TierResolver was not injected. Check that GameLifetimeScope registers TierResolver and CameraFollow.");
            }

            if (_playerTier == null)
            {
                throw new InvalidOperationException(
                    $"{name}: PlayerTier was not injected. Check that GameLifetimeScope registers PlayerTier and CameraFollow.");
            }

            if (_impulse == null)
            {
                throw new InvalidOperationException(
                    $"{name}: CameraImpulse was not injected. Check that GameLifetimeScope registers CameraImpulse and CameraFollow.");
            }

            if (TryGetComponent(out Camera cameraComponent) == false)
            {
                throw new InvalidOperationException(
                    $"{name}: Camera component is not found. CameraFollow must be attached to the camera GameObject.");
            }

            _camera = cameraComponent;
            _baseFov = _camera.fieldOfView;

            _currentOffset = transform.position - _target.position;
            _startOffset = _currentOffset;
        }

        private void OnEnable()
        {
            _playerTier.TierChanged += OnTierChanged;
            ApplyTierOffset(_playerTier.CurrentTier);
        }

        private void OnDisable()
        {
            _playerTier.TierChanged -= OnTierChanged;
        }

        private void LateUpdate()
        {
            float offsetLength = Mathf.Max(1f, _currentOffset.magnitude - _impulse.Pull);
            Vector3 desiredPosition = _target.position + _currentOffset.normalized * offsetLength;

            transform.position = Vector3.SmoothDamp(
                transform.position,
                desiredPosition,
                ref _positionVelocity,
                _positionSmoothTime,
                _maxPositionSpeed) + GetShakeOffset();

            _camera.fieldOfView = _baseFov + _impulse.FovKick;
        }

        private Vector3 GetShakeOffset()
        {
            if (_impulse.Shake <= 0.001f)
            {
                return Vector3.zero;
            }

            float noiseTime = Time.time * _shakeFrequency;
            float noiseX = Mathf.PerlinNoise(noiseTime, 0f) * 2f - 1f;
            float noiseY = Mathf.PerlinNoise(0f, noiseTime) * 2f - 1f;

            return (transform.right * (noiseX * _shakeAxes.x) + transform.up * (noiseY * _shakeAxes.y))
                * _impulse.Shake;
        }

        private void OnTierChanged(ItemTier previousTier, ItemTier currentTier)
        {
            ApplyTierOffset(currentTier);
        }

        private void ApplyTierOffset(ItemTier tier)
        {
            float cameraOffsetMultiplier = _tierResolver.GetCameraOffsetFor(tier);

            _currentOffset = _startOffset * cameraOffsetMultiplier;
        }
    }
}