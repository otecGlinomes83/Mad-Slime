using System;
using UnityEngine;

namespace Movement
{
    [RequireComponent(typeof(CapsuleCollider))]
    public sealed class Mover : MonoBehaviour
    {
        [SerializeField] private float _defaultSpeed = 4f;
        [SerializeField] private float _smoothTime = 0.12f;

        private const float MoveThreshold = 0.05f;

        private CapsuleCollider _playerCollider;
        private Bounds _bounds;
        private bool _hasBounds;
        private Vector3 _currentVelocity;
        private Vector3 _velocityRef;
        private float _currentSpeed;

        private void Awake()
        {
            _playerCollider = GetComponent<CapsuleCollider>();
            _currentSpeed = _defaultSpeed;
        }

        public void SetDefaultSpeed(float speed)
        {
            if (speed <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(speed),
                    "Mover.SetDefaultSpeed requires a positive speed.");
            }

            _defaultSpeed = speed;
            _currentSpeed = speed;
        }

        public void SetSmoothTime(float smoothTime)
        {
            if (smoothTime <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(smoothTime),
                    "Mover.SetSmoothTime requires a positive smooth time.");
            }

            _smoothTime = smoothTime;
        }

        public void SetBounds(Bounds bounds)
        {
            if (bounds.size.x <= 0f || bounds.size.z <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(bounds),
                    "Mover.SetBounds requires positive XZ size.");
            }

            _bounds = bounds;
            _hasBounds = true;
        }

        public void Move(Vector3 direction)
        {
            if (_hasBounds == false)
            {
                throw new InvalidOperationException(
                    $"{name}: Move is called before SetBounds. Drag the Mover into the _mover field of LevelGenerator.");
            }

            if (direction.sqrMagnitude < MoveThreshold * MoveThreshold)
            {
                return;
            }

            direction = direction.normalized;
            Vector3 targetVelocity = direction * _currentSpeed;

            _currentVelocity = Vector3.SmoothDamp
            (
                _currentVelocity,
                targetVelocity,
                ref _velocityRef,
                _smoothTime
            );

            transform.position += _currentVelocity * Time.deltaTime;

            ClampToBounds();
        }

        private void ClampToBounds()
        {
            float radius = _playerCollider.radius;
            Vector3 position = transform.position;

            position.x = Mathf.Clamp(position.x, _bounds.min.x + radius, _bounds.max.x - radius);
            position.z = Mathf.Clamp(position.z, _bounds.min.z + radius, _bounds.max.z - radius);

            transform.position = position;
        }
    }
}
