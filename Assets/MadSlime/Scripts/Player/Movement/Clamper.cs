using System;
using Player;
using UnityEngine;

namespace Movement
{
    public class Clamper : MonoBehaviour, IRadiusRecipient
    {
        private Bounds _bounds;
        private float _radius;
        private bool _hasBounds;
        private bool _hasRadius;

        public void SetBounds(Bounds bounds)
        {
            if (bounds.size.x <= 0f || bounds.size.z <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(bounds),
                    "Clamper.SetBounds requires positive XZ size.");
            }

            _bounds = bounds;
            _hasBounds = true;
        }

        public void SetRadius(float radius)
        {
            if (radius < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(radius),
                    "Clamper.SetRadius requires a non-negative radius.");
            }

            _radius = radius;
            _hasRadius = true;
        }

        public Vector3 ClampPosition(Vector3 desiredPosition)
        {
            if (_hasBounds == false)
            {
                throw new InvalidOperationException(
                    $"{name}: ClampPosition is called before SetBounds. Level preparation must pass the floor bounds.");
            }

            if (_hasRadius == false)
            {
                throw new InvalidOperationException(
                    $"{name}: ClampPosition is called before SetRadius. The player scaler must pass the current radius.");
            }

            Vector3 clamped = desiredPosition;

            float allowedRadiusX = Mathf.Min(_radius, _bounds.extents.x);
            float allowedRadiusZ = Mathf.Min(_radius, _bounds.extents.z);
            clamped.x = Mathf.Clamp(clamped.x, _bounds.min.x + allowedRadiusX, _bounds.max.x - allowedRadiusX);
            clamped.z = Mathf.Clamp(clamped.z, _bounds.min.z + allowedRadiusZ, _bounds.max.z - allowedRadiusZ);

            return clamped;
        }
    }
}
