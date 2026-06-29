using System;
using Game;
using Interfaces;
using UnityEngine;

namespace Skills
{
    public sealed class AttractSkill : MonoBehaviour
    {
        private const float MinDistanceSqr = 0.0001f;

        [SerializeField] private SkillTracker _skillManager;
        [SerializeField] private AttractableDetector _detector;
        [SerializeField] private Timer _timer;
        [SerializeField] private float _attractionForce = 6f;
        [SerializeField] private float _activeDuration = 3f;
        [SerializeField] private float _cooldown = 8f;

        private bool _isActive;
        private bool _isOnCooldown;

        public event Action Started;
        public event Action Ended;
        public event Action CooldownEnded;

        public bool IsActive => _isActive;
        public bool IsOnCooldown => _isOnCooldown;

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

        public void Activate()
        {
            if (_isActive == true || _isOnCooldown == true)
            {
                return;
            }

            if (_skillManager.IsUnlocked(SkillId.Attract) == false)
            {
                return;
            }

            _isActive = true;
            _timer.Setup(_activeDuration);
            _timer.StartCount();
            Started?.Invoke();
        }

        private void OnDetected(IAttractable attractable)
        {
            if (_isActive == false)
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

            target.position += toTarget.normalized * (_attractionForce * Time.deltaTime);
        }

        private void OnTimerFinished()
        {
            if (_isActive == true)
            {
                _isActive = false;
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