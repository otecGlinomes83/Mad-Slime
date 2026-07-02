using System;
using Game;
using UnityEngine;

namespace Assets.Scripts.HealthSystem
{
    public sealed class Healer : MonoBehaviour
    {
        [SerializeField] private Health _health;
        [SerializeField] private Invulnerability _invulnerability;
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
                    $"{name}: Health component is missing. Attach a Health component to the same GameObject.");
            }

            if (_invulnerability == null)
            {
                throw new InvalidOperationException(
                    $"{name}: Invulnerability is not assigned. Drag an Invulnerability component into the _invulnerability field.");
            }

            if (_timer == null)
            {
                throw new InvalidOperationException(
                    $"{name}: Timer is not assigned. Drag a Timer component into the _timer field.");
            }
        }

        private void OnEnable()
        {
            _invulnerability.WindowEnded += OnWindowEnded;
            _timer.Finished += OnTimerFinished;
            _health.Died += OnDied;
        }

        private void OnDisable()
        {
            _invulnerability.WindowEnded -= OnWindowEnded;
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

        private void OnWindowEnded()
        {
            if (_health.Value >= _health.MaxValue)
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