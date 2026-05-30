using System;
using System.Collections.Generic;
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

        private readonly List<ISpawner> _spawners = new List<ISpawner>();
        private bool _isFinished;

        public event Action GameFinished;

        private void Awake()
        {
            _timer.Setup(_timerDuration);

            CollectSpawners();

            _timer.TimerFinished += OnTimerFinished;
            _quotaTreker.QuotaCompleted += OnQuotaCompleted;

            _timer.StartCount();
            StartSpawners();
        }

        private void OnDisable()
        {
            _timer.TimerFinished -= OnTimerFinished;
            _quotaTreker.QuotaCompleted -= OnQuotaCompleted;
        }

        public void Pause()
        {
            _timer.Stop();
            Time.timeScale = 0f;
        }

        public void Resume()
        {
            _timer.Continue();
            Time.timeScale = 1f;
        }

        private void OnTimerFinished()
        {
            if (_isFinished == true)
            {
                return;
            }

            _isFinished = true;
            StopSpawners();
            Pause();
            
            GameFinished?.Invoke();
        }

        private void OnQuotaCompleted()
        {
            if (_isFinished == true)
            {
                return;
            }

            _isFinished = true;
            _timer.Stop();
            StopSpawners();
            Pause();
            
            GameFinished?.Invoke();
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
            foreach (ISpawner spawner in _spawners)
            {
                spawner.StartSpawn();
            }
        }

        private void StopSpawners()
        {
            foreach (ISpawner spawner in _spawners)
            {
                spawner.StopSpawn();
            }
        }
    }
}