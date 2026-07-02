using System;
using Game;
using UnityEngine;

namespace Skills
{
    public sealed class SprintSkill : MonoBehaviour
    {
        [SerializeField] private SkillTracker _skillManager;
        [SerializeField] private Mover _mover;
        [SerializeField] private Timer _timer;
        [SerializeField] private float _speedMultiplier = 2f;
        [SerializeField] private float _activeDuration = 5f;
        [SerializeField] private float _cooldown = 10f;

        private bool _isActive;
        private bool _isOnCooldown;

        public event Action Started;
        public event Action Ended;
        public event Action CooldownEnded;

        public bool IsActive => _isActive;
        public bool IsOnCooldown => _isOnCooldown;

        private void OnEnable()
        {
            _timer.Finished += OnTimerFinished;
        }

        private void OnDisable()
        {
            _timer.Finished -= OnTimerFinished;
        }

        public void Activate()
        {
            if (_isActive == true || _isOnCooldown == true)
            {
                return;
            }

            if (_skillManager.IsUnlocked(SkillId.Sprint) == false)
            {
                return;
            }

            _isActive = true;
            _mover.SetSpeedMultiplier(_speedMultiplier);
            _timer.Setup(_activeDuration);
            _timer.StartCount();
            Started?.Invoke();
        }

        private void OnTimerFinished()
        {
            if (_isActive == true)
            {
                _isActive = false;
                _mover.ResetSpeed();
                _isOnCooldown = true;
                _timer.Setup(_cooldown);
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