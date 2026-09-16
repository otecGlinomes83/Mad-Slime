using Interfaces;
using Player;
using Skills;
using System;
using UnityEngine;

namespace Collectables
{
    public sealed class ItemAttractor : MonoBehaviour
    {
        private const float MinDistanceSqr = 0.0001f;

        [SerializeField] private AttractConfig _config;
        [SerializeField] private PlayerTier _playerTier;
        [SerializeField] private AttractableDetector _detector;

        private void Awake()
        {
            if (_config == null)
            {
                throw new InvalidOperationException(
                    $"{name}: AttractConfig is not assigned. Drag an AttractConfig asset into the _config field.");
            }

            if (_playerTier == null)
            {
                throw new InvalidOperationException(
                    $"{name}: PlayerTier is not assigned. Drag a PlayerTier component into the _playerTier field.");
            }

            if (_detector == null)
            {
                throw new InvalidOperationException(
                    $"{name}: AttractableDetector is not assigned. Drag an AttractableDetector component into the _detector field.");
            }

            if (_config.ApproachMultiplier < 1f)
            {
                throw new InvalidOperationException(
                    $"{name}: AttractConfig '{_config.name}' has ApproachMultiplier < 1. " +
                    "It is the pull speed multiplier at the player's center, so it must be 1 or greater.");
            }

            if (_config.ApproachPower <= 0f)
            {
                throw new InvalidOperationException(
                    $"{name}: AttractConfig '{_config.name}' has ApproachPower <= 0. " +
                    "It must be positive: 1 = linear, 2 = parabola, higher values approach exponential growth.");
            }
        }

        private void OnEnable()
        {
            _detector.Detected += OnAttractableDetected;
        }

        private void OnDisable()
        {
            _detector.Detected -= OnAttractableDetected;
        }

        private void OnAttractableDetected(IAttractable attractable)
        {
            if (attractable.Tier > _playerTier.CurrentTier)
            {
                return;
            }

            Transform target = attractable.Self;
            Vector3 toPlayer = transform.position - target.position;
            toPlayer.y = 0f;

            float sqrDistance = toPlayer.sqrMagnitude;

            if (sqrDistance < MinDistanceSqr)
            {
                return;
            }

            float distance = Mathf.Sqrt(sqrDistance);
            float approach = 1f - Mathf.Clamp01(distance / _detector.Radius);
            float multiplier = 1f + (_config.ApproachMultiplier - 1f) * Mathf.Pow(approach, _config.ApproachPower);

            target.position += toPlayer.normalized * (_config.AttractionForce * multiplier * Time.deltaTime);
        }
    }
}
