using System;
using Collectables;
using Game;
using Interfaces;
using Player;
using UnityEngine;

namespace Skills
{
    public sealed class AttractSkill : BaseSkill
    {
        private const float MinDistanceSqr = 0.0001f;

        [SerializeField] private AttractConfig _config;
        [SerializeField] private PlayerTier _playerTier;
        [SerializeField] private AttractableDetector _detector;
        [SerializeField] private Timer _timer;

        private bool _isActive;
        private bool _isOnCooldown;

        public event Action Started;
        public event Action Ended;
        public event Action CooldownEnded;

        public bool IsActive => _isActive;
        public bool IsOnCooldown => _isOnCooldown;

        private void Awake()
        {
            if (_config == null)
            {
                throw new InvalidOperationException(
                    $"{name}: AttractConfig is not assigned. Drag an AttractConfig asset into the _config field.");
            }
        }

        private void OnEnable()
        {
            _detector.Detected += OnDetected;
            _timer.Finished += OnTimerFinished;
        }

        private void OnDisable()
        {
            _detector.Detected -= OnDetected;
            _timer.Finished -= OnTimerFinished;
        }

        public override bool TryActivate()
        {
            if (_isActive == true || _isOnCooldown == true)
            {
                return false;
            }

            _isActive = true;
            _timer.Setup(_config.ActiveDuration);
            _timer.StartCount();
            Started?.Invoke();

            return true;
        }

        private void OnDetected(IAttractable attractable)
        {
            if (_isActive == false)
            {
                return;
            }

            if (attractable.Tier > _playerTier.CurrentTier)
            {
                return;
            }

            Transform target = attractable.Self;
            Vector3 toTarget = transform.position - target.position;
            toTarget.y = 0f;

            if (toTarget.sqrMagnitude < MinDistanceSqr)
            {
                return;
            }

            target.position += toTarget.normalized * (_config.AttractionForce * Time.deltaTime);
        }

        private void OnTimerFinished()
        {
            if (_isActive == true)
            {
                _isActive = false;
                _isOnCooldown = true;
                _timer.Setup(_config.Cooldown);
                _timer.StartCount();
                Ended?.Invoke();
            }
            else if (_isOnCooldown == true)
            {
                _isOnCooldown = false;
                CooldownEnded?.Invoke();
            }
        }
    }
}
