using System;
using System.Collections.Generic;
using PlayerInput;
using Spawners;
using UnityEngine;

namespace Game
{
    public sealed class SessionHandler : MonoBehaviour
    {
        [SerializeField] private Timer _timer;
        [SerializeField] private float _timerDuration;
        [SerializeField] private QuotaTreker _quotaTreker;
        [SerializeField] private MonoBehaviour[] _spawnerSources;
        [SerializeField] private PlayerInputReader _inputReader;

        private readonly List<ISpawner> _spawners = new List<ISpawner>();
        private bool _isStarted;
        private bool _isFinished;

        private void Awake()
        {
            _timer.Setup(_timerDuration);
            CollectSpawners();
        }

        private void OnEnable()
        {
            _inputReader.MovementKeyPressed += Begin;
            _timer.Finished += OnFinished;
            _quotaTreker.QuotaCompleted += OnQuotaCompleted;
        }

        private void OnDisable()
        {
            _inputReader.MovementKeyPressed -= Begin;
            _timer.Finished -= OnFinished;
            _quotaTreker.QuotaCompleted -= OnQuotaCompleted;
        }

        public void Begin()
        {
            if (_isStarted == true || _isFinished == true)
            {
                return;
            }

            _isStarted = true;
            _timer.StartCount();
            StartSpawners();
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
            StopSpawners();
            _timer.Stop();
            Time.timeScale = 0f;
        }

        private void CollectSpawners()
        {
            for (int i = 0; i < _spawnerSources.Length; i++)
            {
                MonoBehaviour source = _spawnerSources[i];

                if (source is ISpawner spawner)
                {
                    _spawners.Add(spawner);
                    spawner.Setup();
                }
                else
                {
                    throw new InvalidOperationException($"SessionHandler: {source.name} does not implement ISpawner.");
                }
            }
        }

        private void StartSpawners()
        {
            for (int i = 0; i < _spawners.Count; i++)
            {
                _spawners[i].StartSpawn();
            }
        }

        private void StopSpawners()
        {
            for (int i = 0; i < _spawners.Count; i++)
            {
                _spawners[i].StopSpawn();
            }
        }
    }
}
