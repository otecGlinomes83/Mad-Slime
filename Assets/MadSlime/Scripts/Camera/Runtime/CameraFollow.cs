using System;
using Scriptables;
using UnityEngine;

namespace CameraSystem
{
    public class CameraFollow : MonoBehaviour
    {
        [SerializeField] private Transform _target;
        [SerializeField] private CameraConfig _config;
        [SerializeField] private CameraPullAnimator _pullAnimator;
        [SerializeField] private CameraShakeAnimator _shakeAnimator;
        [SerializeField] private CameraFovKickAnimator _fovKickAnimator;

        private Vector3 _positionVelocity;
        private Vector3 _currentOffset;
        private Vector3 _startOffset;
        private Camera _camera;
        private bool _isStopped;

        private void Awake()
        {
            if (_config == null)
            {
                throw new InvalidOperationException(
                    $"{name}: CameraConfig is not assigned. Drag the CameraConfig asset into the _config field.");
            }

            if (_target == null)
            {
                throw new InvalidOperationException(
                    $"{name}: Target is not assigned. Drag a Transform into the _target field in the inspector.");
            }

            if (_pullAnimator == null || _shakeAnimator == null || _fovKickAnimator == null)
            {
                throw new InvalidOperationException(
                    $"{name}: a camera animator is not assigned. Drag the CameraPullAnimator, "
                        + $"CameraShakeAnimator and CameraFovKickAnimator components into the fields.");
            }

            if (TryGetComponent(out Camera cameraComponent) == false)
            {
                throw new InvalidOperationException(
                    $"{name}: Camera component is not found. CameraFollow must be attached to the camera GameObject.");
            }

            _camera = cameraComponent;
            _camera.fieldOfView = _config.BaseFov;

            _currentOffset = transform.position - _target.position;
            _startOffset = _currentOffset;
        }

        public void SetOffsetMultiplier(float offsetMultiplier)
        {
            _currentOffset = _startOffset * offsetMultiplier;
        }

        private void LateUpdate()
        {
            if (_isStopped == true || Time.deltaTime <= 0f)
            {
                return;
            }

            float offsetLength = Mathf.Max(_config.MinDistance, _currentOffset.magnitude - _pullAnimator.Pull);
            Vector3 desiredPosition = _target.position + _currentOffset.normalized * offsetLength;

            transform.position = Vector3.SmoothDamp(
                transform.position,
                desiredPosition,
                ref _positionVelocity,
                _config.PositionSmoothTime,
                _config.MaxPositionSpeed) + GetShakeOffset();

            _camera.fieldOfView = _config.BaseFov + _fovKickAnimator.FovKick;
        }

        public void StopMotion()
        {
            _isStopped = true;
            _positionVelocity = Vector3.zero;

            if (_pullAnimator != null)
            {
                _pullAnimator.enabled = false;
            }

            if (_shakeAnimator != null)
            {
                _shakeAnimator.enabled = false;
            }

            if (_fovKickAnimator != null)
            {
                _fovKickAnimator.enabled = false;
            }
        }

        private Vector3 GetShakeOffset()
        {
            if (_shakeAnimator.Shake <= 0.001f)
            {
                return Vector3.zero;
            }

            float noiseTime = Time.time * _config.ShakeFrequency;
            float noiseX = Mathf.PerlinNoise(noiseTime, 0f) * 2f - 1f;
            float noiseY = Mathf.PerlinNoise(0f, noiseTime) * 2f - 1f;

            return (transform.right * (noiseX * _config.ShakeAxes.x) + transform.up * (noiseY * _config.ShakeAxes.y))
                * _shakeAnimator.Shake;
        }
    }
}
