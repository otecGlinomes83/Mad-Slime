using Interfaces;
using Player;
using Skills;
using System;
using UnityEngine;
using Upgrades;
using VContainer;

namespace Collectables
{
    public sealed class ItemAttractor : MonoBehaviour
    {
        private const float MinDistanceSqr = 0.0001f;

        [SerializeField] private AttractConfig _config;

        private PlayerTier _playerTier;
        private AttractableDetector _detector;
        private ItemDetector _collectDetector;
        private PlayerUpgrades _upgrades;

        [Inject]
        public void Construct(PlayerTier playerTier, AttractableDetector detector, ItemDetector collectDetector,
            PlayerUpgrades upgrades)
        {
            _playerTier = playerTier;
            _detector = detector;
            _collectDetector = collectDetector;
            _upgrades = upgrades;
        }

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
                    $"{name}: PlayerTier was not injected. Check that GameLifetimeScope registers PlayerTier and ItemAttractor.");
            }

            if (_detector == null)
            {
                throw new InvalidOperationException(
                    $"{name}: AttractableDetector was not injected. Check that GameLifetimeScope registers AttractableDetector and ItemAttractor.");
            }

            if (_collectDetector == null)
            {
                throw new InvalidOperationException(
                    $"{name}: ItemDetector was not injected. Check that GameLifetimeScope registers ItemDetector and ItemAttractor.");
            }

            if (_detector.Radius <= _collectDetector.Radius)
            {
                throw new InvalidOperationException(
                    $"{name}: AttractableDetector radius ({_detector.Radius}) must be greater than ItemDetector radius ({_collectDetector.Radius}). " +
                    "The acceleration ramp lives between them: from the attract edge down to the capture point.");
            }

            if (_config.ApproachMultiplier < 1f)
            {
                throw new InvalidOperationException(
                    $"{name}: AttractConfig '{_config.name}' has ApproachMultiplier < 1. " +
                    "It is the pull speed multiplier at the capture point, so it must be 1 or greater.");
            }

            if (_config.ApproachPower <= 0f)
            {
                throw new InvalidOperationException(
                    $"{name}: AttractConfig '{_config.name}' has ApproachPower <= 0. " +
                    "It must be positive: 1 = linear, 2 = parabola, higher values approach exponential growth.");
            }

            if (_config.OrbitStrength < 0f)
            {
                throw new InvalidOperationException(
                    $"{name}: AttractConfig '{_config.name}' has OrbitStrength < 0. " +
                    "It is the sideways swirl speed as a fraction of the pull speed, so it must be 0 or greater. 0 = straight line.");
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
            if (attractable.Tier > _playerTier.CurrentTier + _upgrades.AmbitionTierOffset)
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
            float approach = 1f - Mathf.Clamp01(
                (distance - _collectDetector.Radius) / (_detector.Radius - _collectDetector.Radius));
            float multiplier = 1f + (_config.ApproachMultiplier - 1f) * Mathf.Pow(approach, _config.ApproachPower);
            float speed = _config.AttractionForce * multiplier;

            Vector3 radial = toPlayer / distance;
            int instanceId = target.GetInstanceID();
            float orbitSide = 1f - 2f * (instanceId & 1);
            Vector3 tangent = new Vector3(radial.z, 0f, -radial.x) * (orbitSide * _config.OrbitStrength);

            target.position += (radial + tangent) * (speed * Time.deltaTime);
        }
    }
}