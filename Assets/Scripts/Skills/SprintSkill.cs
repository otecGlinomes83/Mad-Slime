using System;
using Game;
using Movement;
using UnityEngine;

namespace Skills
{
    public sealed class SprintSkill : BaseSkill
    {
        [SerializeField] private SprintConfig _config;
        [SerializeField] private Mover _mover;
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
                    $"{name}: SprintConfig is not assigned. Drag a SprintConfig asset into the _config field.");
            }
        }

        private void OnEnable()
        {
            _timer.Finished += OnTimerFinished;
        }

        private void OnDisable()
        {
            _timer.Finished -= OnTimerFinished;
        }

        public override bool TryActivate()
        {
            if (_isActive == true || _isOnCooldown == true)
            {
                return false;
            }

            _isActive = true;
            _mover.SetSpeedMultiplier(_config.SpeedMultiplier);
            _timer.Setup(_config.Duration);
            _timer.StartCount();
            Started?.Invoke();

            return true;
        }

        private void OnTimerFinished()
        {
            if (_isActive == true)
            {
                _isActive = false;
                _mover.ResetSpeed();
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
