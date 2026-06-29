using System;
using Game;
using UnityEngine;

namespace Assets.Scripts.HealthSystem
{
    public sealed class Healer : MonoBehaviour
    {
        [SerializeField] private Health _health;
        [SerializeField] private Timer _timer;
        [SerializeField] private float _regenDelay = 5f;

        private void Awake()
        {
            if (_health == null)
            {
                _health = GetComponent<Health>();
            }

            if (_health == null)
            {
                throw new InvalidOperationException(
                    $"{name}: HealthSystem component is missing. Attach a HealthSystem component to the same GameObject.");
            }

            if (_timer == null)
            {
                throw new InvalidOperationException(
                    $"{name}: Timer is not assigned. Drag a Timer component into the _timer field in the inspector.");
            }
        }

        private void OnEnable()
        {
            _health.InvulnerabilityEnded += OnInvulnerabilityEnded;
            _timer.Finished += OnTimerFinished;
            _health.Died += OnDied;
        }

        private void OnDisable()
        {
            _health.InvulnerabilityEnded -= OnInvulnerabilityEnded;
            _timer.Finished -= OnTimerFinished;
        }

        public void Heal()
        {
            _health.Heal(_health.MaxValue);
        }

        private void OnDied()
        {
            _timer.Stop();
        }

        private void OnInvulnerabilityEnded()
        {
            if(_health.Value>=_health.MaxValue)
            {
                return;
            }

            _timer.Stop();
            _timer.Setup(_regenDelay);
            _timer.StartCount();
        }

        private void OnTimerFinished()
        {
            _health.Heal(_health.MaxValue);
        }
    }
}