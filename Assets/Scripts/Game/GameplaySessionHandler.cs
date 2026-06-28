using Assets.Scripts.HealthSystem;
using PlayerInput;
using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game
{
    public sealed class GameplaySessionHandler : MonoBehaviour
    {
        [SerializeField] private Health _health;
        [SerializeField] private Healer _healer;
        [SerializeField] private LevelTransitor _levelTransitor;
        [SerializeField] private Timer _timer;
        [SerializeField] private float _timerDuration;
        [SerializeField] private QuotaTracker _quotaTracker;
        [SerializeField] private PlayerInputReader _inputReader;
        [SerializeField] private Pauser _pauser;

        private bool _isStarted;
        private bool _isFinished;

        public event Action PlayerDied;

        private void Awake()
        {
            _timer.Setup(_timerDuration);
            _pauser.RequestPause();
        }

        private void OnEnable()
        {
            _inputReader.MovementKeyPressed += Begin;
            _timer.Finished += OnTimeOut;
            _quotaTracker.QuotaCompleted += OnQuotaCompleted;

            _health.Died += OnPlayerDied;
        }

        private void OnDisable()
        {
            _inputReader.MovementKeyPressed -= Begin;
            _timer.Finished -= OnTimeOut;
            _quotaTracker.QuotaCompleted -= OnQuotaCompleted;
            _health.Died -= OnPlayerDied;
        }

        public void Revive()
        {
            _healer.Heal();
            _health.TurnOnInvulnerabilityWindow(5f);
        }

        public void Restart()
        {
            _levelTransitor.Restart();
        }

        private void OnPlayerDied()
        {
            PlayerDied?.Invoke();
        }

        private void Begin()
        {
            if (_isStarted == true || _isFinished == true)
            {
                return;
            }

            _pauser.RequestResume();

            _isStarted = true;
            _timer.StartCount();
        }


        private void OnTimeOut()
        {
            if (_isFinished == true)
            {
                return;
            }

            _isFinished = true;
            FinishGame();
        }

        private void OnQuotaCompleted()
        {
            if (_isFinished == true)
            {
                return;
            }

            _isFinished = true;
            FinishGame();
        }

        private void FinishGame()
        {
            _timer.Stop();
            _levelTransitor.LoadNext();
        }
    }
}