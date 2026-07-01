using Assets.Scripts.HealthSystem;
using PlayerInput;
using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using YG;

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
        public event Action GameStarted;

        private void Awake()
        {
            TrySetCurrentLevelFromScene();
            _timer.Setup(_timerDuration);
            _pauser.RequestPause();
        }

        private static void TrySetCurrentLevelFromScene()
        {
            string sceneName = SceneManager.GetActiveScene().name;

            if (sceneName.StartsWith("Level") == false)
            {
                return;
            }

            string numberPart = sceneName.Substring("Level".Length);

            if (int.TryParse(numberPart, out int level) == false)
            {
                return;
            }

            if (level <= 0)
            {
                return;
            }

            YG2.saves.CurrentLevel = level;
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
            GameStarted?.Invoke();
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