using Camera;
using PlayerInput;
using ShapeFill;
using System;
using System.Collections.Generic;
using UI;
using UnityEngine;

namespace Game
{
    public sealed class SessionHandler : MonoBehaviour
    {
        [SerializeField] private Timer _timer;
        [SerializeField] private ShapeFillOrchestrator _shapeFillOrchestrator;
        [SerializeField] private FillCounter _fillCounter;
        [SerializeField] private QuotaGenerator _quotaGenerator;
        [SerializeField] private float _timerDuration;
        [SerializeField] private QuotaTracker _quotaTracker;
        [SerializeField] private QuotaUI _quotaUI;
        [SerializeField] private CameraArriver _cameraArriver;
        [SerializeField] private MonoBehaviour[] _spawnerSources;
        [SerializeField] private PlayerInputReader _inputReader;

      //  [SerializeField] private CubeTweener _cubeTweener;

        private readonly List<ISpawner> _spawners = new List<ISpawner>();
        private bool _isStarted;
        private bool _isFinished;

        private void Awake()
        {
            _timer.Setup(_timerDuration);
            CollectSpawners();

            if (_cameraArriver == null)
            {
                throw new InvalidOperationException(
                    $"{name}: CameraArriver is not assigned. Drag a CameraArriver into the _cameraArriver field in the inspector.");
            }
        }

        private void OnEnable()
        {
            _inputReader.MovementKeyPressed += Begin;
            _timer.Finished += OnFinished;
            _quotaTracker.QuotaCompleted += OnQuotaCompleted;
            _cameraArriver.Arrived += OnCameraArrived;
       //     _cubeTweener.AnimationFinished += OnAnimationFinished;
        }

        private void OnDisable()
        {
            _inputReader.MovementKeyPressed -= Begin;
            _timer.Finished -= OnFinished;
            _quotaTracker.QuotaCompleted -= OnQuotaCompleted;
            _cameraArriver.Arrived -= OnCameraArrived;
         //   _cubeTweener.AnimationFinished -= OnAnimationFinished;
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
            _quotaTracker.Initialize(_quotaGenerator.Generate(_shapeFillOrchestrator.RequiredFillCount));
            _quotaUI.Setup(_quotaTracker);
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
            //_cubeTweener.MoveCube();
            _cameraArriver.Move();
        }

        //private void OnAnimationFinished()
        //{
        //}

        private void OnCameraArrived()
        {
            int fillCount = _fillCounter.Calculate(_shapeFillOrchestrator.RequiredFillCount);
            _shapeFillOrchestrator.StartFilling(fillCount);
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
