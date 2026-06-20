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
        private bool _isFlying;

        public event Action<FlyingCube> Arrived;

        public void Launch(Vector3 target, float duration)
        {
            _targetPosition = target;
            _startPosition = transform.position;
            _flightDuration = duration;
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
