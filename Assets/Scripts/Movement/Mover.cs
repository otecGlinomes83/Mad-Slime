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

        public Vector3 Velocity => _currentVelocity;
        public float ActualSpeed => _currentVelocity.magnitude;

        public event Action SpeedChanged;

        private void Awake()
        {
            _moveChecker = GetComponent<MoveChecker>();
            _currentSpeed = _defaultSpeed;
        }

        public void SetDefaultSpeed(float speed)
        {
            if (speed <= 0f)
            {
                throw new Exception("Speed must be greater than 0");
            }
            
            _defaultSpeed = speed;
        }
        
        public void SetSpeedMultiplier(float multiplier)
        {
            if (multiplier <= 0f)
            {
                return;
            }

            _currentSpeed = _defaultSpeed * multiplier;
            SpeedChanged?.Invoke();
        }

        public void ResetSpeed()
        {
            _currentSpeed = _defaultSpeed;
            SpeedChanged?.Invoke();
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