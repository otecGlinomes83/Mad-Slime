using System;
using UnityEngine;
using Random = UnityEngine.Random;

namespace ShapeFill
{
    public sealed class FlyingCube : MonoBehaviour
    {
        private enum State
        {
            Idle,
            Growing,
            Flying,
            Settling
        }

        [Header("Flight")]
        [SerializeField, Range(0f, 0.6f)] private float _maxStretch = 0.35f;
        [SerializeField, Range(0f, 1f)] private float _stretchSqueeze = 0.5f;
        [SerializeField, Range(0f, 0.6f)] private float _arcFraction = 0.25f;
        [SerializeField, Min(0f)] private float _minRollSpeed = 240f;
        [SerializeField, Min(0f)] private float _maxRollSpeed = 480f;
        [SerializeField, Range(0f, 0.99f)] private float _rotationSettleStart = 0.75f;

        [Header("Landing")]
        [SerializeField, Range(0f, 0.6f)] private float _landingPunch = 0.18f;
        [SerializeField, Min(0.01f)] private float _settleDuration = 0.18f;
        [SerializeField, Range(0f, 2f)] private float _settleSquashSpread = 0.6f;

        [Header("Grow")]
        [SerializeField, Min(0.01f)] private float _growDuration = 0.22f;

        private State _state = State.Idle;

        private Vector3 _startPosition;
        private Vector3 _controlPosition;
        private Vector3 _targetPosition;
        private Vector3 _targetScale;
        private Vector3 _spinAxis;
        private float _flightDuration;
        private float _elapsedTime;
        private float _rollAngle;
        private float _rollSpeed;

        public event Action<FlyingCube> Arrived;

        private void Awake()
        {
            if (_minRollSpeed > _maxRollSpeed)
            {
                throw new InvalidOperationException(
                    $"{name}: MinRollSpeed {_minRollSpeed} is greater than MaxRollSpeed {_maxRollSpeed}.");
            }
        }

        public void Launch(Vector3 target, float duration)
        {
            _targetPosition = target;
            _startPosition = transform.position;
            _targetScale = transform.localScale;
            _controlPosition = CalculateControlPosition(_startPosition, target);
            _spinAxis = Random.onUnitSphere;
            _rollAngle = 0f;
            _rollSpeed = Random.Range(_minRollSpeed, _maxRollSpeed);
            _flightDuration = duration;
            _elapsedTime = 0f;
            _state = State.Flying;
        }

        public void GrowIn()
        {
            _targetScale = transform.localScale;
            transform.localScale = Vector3.zero;
            _elapsedTime = 0f;
            _state = State.Growing;
        }

        private void Update()
        {
            switch (_state)
            {
                case State.Growing:
                    UpdateGrowing();
                    break;

                case State.Flying:
                    UpdateFlying();
                    break;

                case State.Settling:
                    UpdateSettling();
                    break;
            }
        }

        private void UpdateGrowing()
        {
            _elapsedTime += Time.deltaTime;
            float progress = Mathf.Clamp01(_elapsedTime / _growDuration);
            float easedProgress = 1f - (1f - progress) * (1f - progress);

            transform.localScale = _targetScale * easedProgress;

            if (progress >= 1f)
            {
                transform.localScale = _targetScale;
                _state = State.Idle;
            }
        }

        private void UpdateFlying()
        {
            _elapsedTime += Time.deltaTime;
            float progress = Mathf.Clamp01(_elapsedTime / _flightDuration);
            float smoothedProgress = progress * progress * (3f - 2f * progress);

            Vector3 previousPosition = transform.position;
            transform.position = GetBezierPoint(_startPosition, _controlPosition, _targetPosition, smoothedProgress);
            Vector3 velocity = transform.position - previousPosition;

            ApplyFlightOrientation(velocity, progress);
            ApplyFlightStretch(progress);

            if (progress < 1f)
            {
                return;
            }

            transform.position = _targetPosition;
            transform.rotation = Quaternion.identity;
            transform.localScale = _targetScale;

            _elapsedTime = 0f;
            _state = State.Settling;

            Arrived?.Invoke(this);
        }

        private void ApplyFlightOrientation(Vector3 velocity, float progress)
        {
            if (velocity.sqrMagnitude < 0.000001f)
            {
                return;
            }

            _rollAngle += _rollSpeed * Time.deltaTime;

            Quaternion flightRotation = Quaternion.LookRotation(velocity.normalized, Vector3.up)
                * Quaternion.AngleAxis(_rollAngle, Vector3.forward);

            float settle = Mathf.Clamp01(Mathf.InverseLerp(_rotationSettleStart, 1f, progress));

            transform.rotation = Quaternion.Slerp(flightRotation, Quaternion.identity, settle);
        }

        private void ApplyFlightStretch(float progress)
        {
            float stretch = Mathf.Sin(progress * Mathf.PI) * _maxStretch;
            float squeeze = 1f - stretch * _stretchSqueeze;

            transform.localScale = new Vector3(
                _targetScale.x * squeeze,
                _targetScale.y * squeeze,
                _targetScale.z * (1f + stretch));
        }

        private void UpdateSettling()
        {
            _elapsedTime += Time.deltaTime;
            float progress = Mathf.Clamp01(_elapsedTime / _settleDuration);
            float punch = _landingPunch * (1f - progress);

            transform.localScale = new Vector3(
                _targetScale.x * (1f + punch * _settleSquashSpread),
                _targetScale.y * (1f - punch),
                _targetScale.z * (1f + punch * _settleSquashSpread));

            if (progress >= 1f)
            {
                transform.localScale = _targetScale;
                _state = State.Idle;
            }
        }

        private Vector3 CalculateControlPosition(Vector3 startPosition, Vector3 endPosition)
        {
            Vector3 offset = endPosition - startPosition;
            float distance = offset.magnitude;

            if (distance < 0.001f)
            {
                return Vector3.Lerp(startPosition, endPosition, 0.5f);
            }

            Vector3 perpendicular = Vector3.Cross(offset / distance, Random.onUnitSphere);

            if (perpendicular.sqrMagnitude < 0.001f)
            {
                perpendicular = Vector3.up;
            }
            else
            {
                perpendicular.Normalize();
            }

            float side = 1f;

            if (Random.value < 0.5f)
            {
                side = -1f;
            }

            return Vector3.Lerp(startPosition, endPosition, 0.5f) + perpendicular * (distance * _arcFraction * side);
        }

        private static Vector3 GetBezierPoint(Vector3 start, Vector3 control, Vector3 end, float progress)
        {
            float invertedProgress = 1f - progress;

            return invertedProgress * invertedProgress * start
                + 2f * invertedProgress * progress * control
                + progress * progress * end;
        }
    }
}
