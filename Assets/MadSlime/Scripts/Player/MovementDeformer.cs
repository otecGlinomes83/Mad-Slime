using Movement;
using Scriptables;
using System;
using UnityEngine;
using VContainer;

namespace Player
{
    public sealed class MovementDeformer : MonoBehaviour
    {
        [SerializeField] private PlayerConfig _config;

        private Mover _mover;
        private Vector3 _baseScale;
        private float _stretchVelocityRef;
        private float _stretch;

        [Inject]
        public void Construct(PlayerConfig config, Mover mover)
        {
            _config = config;
            _mover = mover;
        }

        private void Awake()
        {
            if (_config == null)
            {
                throw new InvalidOperationException(
                    $"{name}: PlayerConfig is not assigned. Drag the PlayerConfig asset into the _config field.");
            }

            if (_mover == null)
            {
                throw new InvalidOperationException(
                    $"{name}: Mover was not injected. Check that GameLifetimeScope registers Mover and MovementDeformer.");
            }

            _baseScale = transform.localScale;
        }

        private void Update()
        {
            float normalizedSpeed = 0f;

            if (_mover.CurrentSpeed > 0f)
            {
                normalizedSpeed = Mathf.Clamp01(_mover.Velocity.magnitude / _mover.CurrentSpeed);
            }

            float targetStretch = normalizedSpeed * _config.DeformMaxStretch;
            _stretch = Mathf.SmoothDamp(_stretch, targetStretch, ref _stretchVelocityRef, _config.DeformSmoothTime);

            float squeeze = 1f - _stretch * _config.DeformSqueeze;

            transform.localScale = new Vector3(
                _baseScale.x * squeeze,
                _baseScale.y * squeeze,
                _baseScale.z * (1f + _stretch));
        }
    }
}
