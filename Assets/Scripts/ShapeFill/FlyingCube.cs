using System;
using UnityEngine;

namespace ShapeFill
{
    public sealed class FlyingCube : MonoBehaviour
    {
        private Vector3 _startPosition;
        private Vector3 _targetPosition;
        private float _flightDuration;
        private float _elapsedTime;
        private Vector3 _spinVelocity;
        private bool _isFlying;

        public event Action<FlyingCube> Arrived;

        public void Launch(Vector3 target, float duration, Vector3 spinSpeed)
        {
            _targetPosition = target;
            _startPosition = transform.position;
            _flightDuration = duration;
            _spinVelocity = spinSpeed;
            _elapsedTime = 0f;
            _isFlying = true;
        }

        private void Update()
        {
            if (_isFlying == false)
            {
                return;
            }

            _elapsedTime += Time.deltaTime;
            float progress = Mathf.Clamp01(_elapsedTime / _flightDuration);
            float smoothedProgress = progress * progress * (3f - 2f * progress);

            transform.position = Vector3.Lerp(_startPosition, _targetPosition, smoothedProgress);
            transform.Rotate(_spinVelocity * Time.deltaTime, Space.Self);

            if (progress >= 1f)
            {
                _isFlying = false;
                transform.position = _targetPosition;
                transform.rotation = Quaternion.identity;
                Arrived?.Invoke(this);
            }
        }
    }
}
