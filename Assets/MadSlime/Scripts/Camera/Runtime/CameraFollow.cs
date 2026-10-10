using System;
using UnityEngine;
using VContainer;

namespace CameraSystem
{
    public class CameraFollow : MonoBehaviour
    {
        [SerializeField] private Transform _target;
        [SerializeField] private float _positionSmoothTime = 0.25f;
        [SerializeField] private float _maxPositionSpeed = 50f;
        [SerializeField, Min(0.1f)] private float _shakeFrequency = 25f;
        [SerializeField, Range(0f, 1f)] private Vector2 _shakeAxes = new Vector2(1f, 1f);
        [SerializeField] private CameraPullAnimator _pullAnimator;
        [SerializeField] private CameraShakeAnimator _shakeAnimator;
        [SerializeField] private CameraFovKickAnimator _fovKickAnimator;

        private Vector3 _positionVelocity;
        private Vector3 _currentOffset;
        private Vector3 _startOffset;
        private Camera _camera;
        private float _baseFov;

        private void Awake()
        {
            if (_target == null)
            {
                throw new InvalidOperationException(
                    $"{name}: Target is not assigned. Drag a Transform into the _target field in the inspector.");
            }

            if (_pullAnimator == null || _shakeAnimator == null || _fovKickAnimator == null)
            {
                throw new InvalidOperationException(
                    $"{name}: a camera animator is not assigned. Drag the CameraPullAnimator, CameraShakeAnimator and CameraFovKickAnimator components into the fields.");
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

        public void SetOffsetMultiplier(float offsetMultiplier)
        {
            _currentOffset = _startOffset * offsetMultiplier;
        }

        private void LateUpdate()
        {
            float offsetLength = Mathf.Max(1f, _currentOffset.magnitude - _pullAnimator.Pull);
            Vector3 desiredPosition = _target.position + _currentOffset.normalized * offsetLength;

            transform.position = Vector3.SmoothDamp(
                transform.position,
                desiredPosition,
                ref _positionVelocity,
                _positionSmoothTime,
                _maxPositionSpeed) + GetShakeOffset();

            _camera.fieldOfView = _baseFov + _fovKickAnimator.FovKick;
        }

        private Vector3 GetShakeOffset()
        {
            if (_shakeAnimator.Shake <= 0.001f)
            {
                return Vector3.zero;
            }

            float noiseTime = Time.time * _shakeFrequency;
            float noiseX = Mathf.PerlinNoise(noiseTime, 0f) * 2f - 1f;
            float noiseY = Mathf.PerlinNoise(0f, noiseTime) * 2f - 1f;

            return (transform.right * (noiseX * _shakeAxes.x) + transform.up * (noiseY * _shakeAxes.y))
                * _shakeAnimator.Shake;
        }
    }
}
