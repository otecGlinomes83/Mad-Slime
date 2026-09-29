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

        private const float RestStrengthEpsilon = 0.001f;

        private Mover _mover;
        private Vector3 _baseScale;
        private bool _isAtRestScale;

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
            _isAtRestScale = true;
        }

        private void Update()
        {
            float strength = _mover.CrawlStrength;

            if (strength <= RestStrengthEpsilon)
            {
                if (_isAtRestScale == false)
                {
                    _isAtRestScale = true;
                    transform.localScale = _baseScale;
                }

                return;
            }

            _isAtRestScale = false;

            float stretch = strength * _config.DeformMaxStretch * _config.CrawlStretchCurve.Evaluate(_mover.CrawlPhase);
            float squeeze = 1f - stretch * _config.DeformSqueeze;

            transform.localScale = new Vector3(
                _baseScale.x * squeeze,
                _baseScale.y * squeeze,
                _baseScale.z * (1f + stretch));
        }
    }
}
