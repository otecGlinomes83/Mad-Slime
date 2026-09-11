using Collectables;
using Interfaces;
using Player;
using System;
using UnityEngine;

namespace Skills
{
    public sealed class AttractSkill : BaseSkill
    {
        private const float MinDistanceSqr = 0.0001f;

        [SerializeField] private AttractConfig _config;
        [SerializeField] private PlayerTier _playerTier;
        [SerializeField] private AttractableDetector _detector;

        public override SkillConfig Config => _config;

        protected override void Awake()
        {
            base.Awake();

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
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            _detector.Detected += OnAttractableDetected;
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            _detector.Detected -= OnAttractableDetected;
        }

        protected override void OnActivated()
        {
        }

        protected override void OnTick()
        {
        }

        protected override void OnDeactivated()
        {
        }

        private void OnAttractableDetected(IAttractable attractable)
        {
            if (IsActive == false)
            {
                return;
            }

            if (attractable.Tier > _playerTier.CurrentTier)
            {
                return;
            }

            Transform target = attractable.Self;
            Vector3 toPlayer = transform.position - target.position;
            toPlayer.y = 0f;

            if (toPlayer.sqrMagnitude < MinDistanceSqr)
            {
                return;
            }

            target.position += toPlayer.normalized * (_config.AttractionForce * Time.deltaTime);
        }
    }
}
