using Game;
using System;
using UnityEngine;

namespace Skills
{
    public abstract class BaseSkill : MonoBehaviour
    {
        [SerializeField] private Timer _timer;

        private bool _isActive;
        private bool _isOnCooldown;

        public abstract SkillConfig Config { get; }

        public event Action Started;
        public event Action Ended;
        public event Action CooldownEnded;

        public bool IsActive => _isActive;
        public bool IsOnCooldown => _isOnCooldown;

        protected virtual void OnEnable()
        {
            _timer.Finished += OnTimerFinished;
        }

        protected virtual void OnDisable()
        {
            _timer.Finished -= OnTimerFinished;
        }

        public bool TryActivate()
        {
            if (_isActive == true || _isOnCooldown == true)
            {
                return false;
            }

            if (Config.Duration <= 0f)
            {
                OnActivated();
                Started?.Invoke();
                OnDeactivated();
                Ended?.Invoke();
                StartCooldown();
                return true;
            }

            _isActive = true;
            _timer.Setup(Config.Duration);
            _timer.StartCount();

            OnActivated();
            Started?.Invoke();

            return true;
        }

        private void Update()
        {
            if (_isActive == false)
            {
                return;
            }

            OnTick();
        }

        private void OnTimerFinished()
        {
            if (_isActive == true)
            {
                _isActive = false;

                OnDeactivated();
                Ended?.Invoke();

                StartCooldown();
                return;
            }

            _isOnCooldown = false;
            CooldownEnded?.Invoke();
        }

        private void StartCooldown()
        {
            if (Config.Cooldown <= 0f)
            {
                return;
            }

            _isOnCooldown = true;
            _timer.Setup(Config.Cooldown);
            _timer.StartCount();
        }

        protected abstract void OnActivated();

        protected abstract void OnTick();

        protected abstract void OnDeactivated();
    }
}
