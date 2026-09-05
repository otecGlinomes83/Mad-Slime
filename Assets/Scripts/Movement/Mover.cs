using System;
using UnityEngine;

namespace Movement
{
    [RequireComponent(typeof(MoveChecker))]
    public sealed class Mover : MonoBehaviour
    {
        [SerializeField] private float _defaultSpeed = 4f;
        [SerializeField] private float _smoothTime = 0.12f;

        private const float MoveThreshold = 0.05f;

        private MoveChecker _moveChecker;
        private Vector3 _currentVelocity;
        private Vector3 _velocityRef;
        private float _currentSpeed;

        private void Awake()
        {
            _moveChecker = GetComponent<MoveChecker>();
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

        public void Move(Vector3 direction)
        {
            if (direction.sqrMagnitude < MoveThreshold * MoveThreshold)
            {
                return;
            }

            direction = direction.normalized;
            Vector3 targetVelocity = direction * _currentSpeed;

            Vector3 nextVelocity = Vector3.SmoothDamp
            (
                _currentVelocity,
                targetVelocity,
                ref _velocityRef,
                _smoothTime
            );

            if (_moveChecker.IsAbleToMove(transform.position, nextVelocity)==false)
            {
                _currentVelocity = Vector3.zero;
                _velocityRef = Vector3.zero;
                return;
            }

            _currentVelocity = nextVelocity;
            transform.position += _currentVelocity * Time.deltaTime;
        }
    }
}
