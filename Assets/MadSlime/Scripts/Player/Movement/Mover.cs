using System;
using UnityEngine;

namespace Movement
{
    public class Mover : MonoBehaviour
    {
        private Clamper _clamper;
        private Vector3 _velocity;

        public Vector3 Velocity => _velocity;

        public void Setup(Clamper clamper)
        {
            if (clamper == null)
            {
                throw new ArgumentNullException(nameof(clamper));
            }

            _clamper = clamper;
        }

        public void Stop()
        {
            _velocity = Vector3.zero;
        }

        public void Move(Vector3 direction, float speed)
        {
            if (speed < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(speed));
            }

            Vector3 startPosition = transform.position;
            Vector3 stepVelocity = Vector3.ClampMagnitude(direction, 1f) * speed;
            Vector3 desiredPosition = startPosition + stepVelocity * Time.deltaTime;
            transform.position = _clamper.ClampPosition(desiredPosition);
            _velocity = Vector3.zero;

            if (Time.deltaTime > 0f)
            {
                _velocity = (transform.position - startPosition) / Time.deltaTime;
            }
        }
    }
}
