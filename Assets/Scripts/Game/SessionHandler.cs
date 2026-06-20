using PlayerInput;
using System;
using UnityEngine;

namespace Game
{
    public sealed class SessionHandler : MonoBehaviour
    {
        [SerializeField] private Health.Health _health;
        [SerializeField] private Timer _timer;
        [SerializeField] private float _timerDuration;
        [SerializeField] private QuotaTracker _quotaTracker;
        [SerializeField] private PlayerInputReader _inputReader;

        private bool _isStarted;
        private bool _isFinished;

        private void Awake()
        {
            _timer.Setup(_timerDuration);
            Time.timeScale = 0f;
        }

        private void OnEnable()
        {
            _health.Died += OnDie;
            _inputReader.MovementKeyPressed += Begin;
            _timer.Finished += OnFinished;
            _quotaTracker.QuotaCompleted += OnQuotaCompleted;
        }

        private void OnDisable()
        {
            _health.Died -= OnDie;
            _inputReader.MovementKeyPressed -= Begin;
            _timer.Finished -= OnFinished;
            _quotaTracker.QuotaCompleted -= OnQuotaCompleted;
        }

        public void Begin()
        {
            if (_isStarted == true || _isFinished == true)
            {
                return;
            }

            _isStarted = true;
            Time.timeScale = 1f;

            _timer.StartCount();
        }

        public void Pause()
        {
            if (_isStarted == false || _isFinished == true)
            {
                return;
            }

            _timer.Stop();
            Time.timeScale = 0f;
        }

        public void Resume()
        {
            if (_isStarted == false || _isFinished == true)
            {
                return;
            }

            _timer.Continue();
            Time.timeScale = 1f;
        }

        private void OnDie()
        {
            Pause();
            //Покажи юай респауна и дай возможность игроку начать заново
        }

        private void OnFinished()
        {
            if (_isFinished == true)
            {
                return;
            }

            _isFinished = true;
            StopGame();
        }

        private void OnQuotaCompleted()
        {
            if (_isFinished == true)
            {
                return;
            }

            _isFinished = true;
            StopGame();
        }

        private void StopGame()
        {
            _timer.Stop();
        }
    }
}