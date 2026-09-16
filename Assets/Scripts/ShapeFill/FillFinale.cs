using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using Random = UnityEngine.Random;

namespace ShapeFill
{
    public sealed class FillFinale : MonoBehaviour
    {
        [SerializeField] private ShapeFillOrchestrator _orchestrator;
        [SerializeField] private CubeSpawner _spawner;
        [SerializeField] private GridBuilder _gridBuilder;

        [SerializeField, Min(1)] private int _confettiCount = 40;
        [SerializeField, Min(0.1f)] private float _confettiDuration = 1.1f;
        [SerializeField, Min(0.1f)] private float _confettiSpeed = 7f;
        [SerializeField, Min(0f)] private float _confettiGravity = 12f;
        [SerializeField, Range(0f, 3f)] private float _confettiUpBiasMin = 0.6f;
        [SerializeField, Range(0f, 3f)] private float _confettiUpBiasMax = 1.4f;
        [SerializeField, Range(0.1f, 2f)] private float _confettiScaleFactor = 0.7f;
        [SerializeField, Range(0f, 0.9f)] private float _confettiFadeFraction = 0.25f;
        [SerializeField, Range(0f, 0.3f)] private float _shapePunchStrength = 0.06f;
        [SerializeField, Min(0.01f)] private float _shapePunchDuration = 0.4f;
        [SerializeField, Min(0)] private int _shapePunchVibrato = 10;
        [SerializeField, Range(0f, 1f)] private float _shapePunchElasticity = 0.3f;
        [SerializeField, Min(0f)] private float _fovKick = 9f;
        [SerializeField, Min(0.01f)] private float _fovDuration = 0.6f;

        private readonly List<FlyingCube> _confetti = new List<FlyingCube>();
        private readonly List<Vector3> _velocities = new List<Vector3>();
        private readonly List<Vector3> _angularVelocities = new List<Vector3>();

        private float _burstElapsedTime;
        private float _confettiScale;
        private bool _isBursting;
        private Camera _camera;
        private float _startFov;

        private void Awake()
        {
            if (_orchestrator == null)
            {
                throw new InvalidOperationException(
                    $"{name}: Orchestrator is not assigned. Drag a ShapeFillOrchestrator component into the _orchestrator field.");
            }

            if (_spawner == null)
            {
                throw new InvalidOperationException(
                    $"{name}: Spawner is not assigned. Drag a CubeSpawner component into the _spawner field.");
            }

            if (_gridBuilder == null)
            {
                throw new InvalidOperationException(
                    $"{name}: GridBuilder is not assigned. Drag a GridBuilder component into the _gridBuilder field.");
            }

            if (_confettiUpBiasMin > _confettiUpBiasMax)
            {
                throw new InvalidOperationException(
                    $"{name}: ConfettiUpBiasMin {_confettiUpBiasMin} is greater than ConfettiUpBiasMax {_confettiUpBiasMax}.");
            }
        }

        private void OnEnable()
        {
            _orchestrator.FillCompleted += OnFillCompleted;
        }

        private void OnDisable()
        {
            _orchestrator.FillCompleted -= OnFillCompleted;
        }

        private void Update()
        {
            if (_isBursting == false)
            {
                return;
            }

            _burstElapsedTime += Time.deltaTime;
            UpdateConfetti();

            if (_burstElapsedTime >= _confettiDuration)
            {
                StopBurst();
            }
        }

        private void OnFillCompleted(float percent)
        {
            if (percent < 1f)
            {
                return;
            }

            Play();
        }

        public void Play()
        {
            Camera mainCamera = Camera.main;

            if (mainCamera == null)
            {
                throw new InvalidOperationException(
                    $"{name}: Camera.main is not found. Tag the fill scene camera with the MainCamera tag.");
            }

            _camera = mainCamera;
            _startFov = _camera.fieldOfView;

            SpawnConfetti();

            _gridBuilder.transform.DOKill();
            _gridBuilder.transform.DOPunchScale(
                    Vector3.one * _shapePunchStrength,
                    _shapePunchDuration,
                    _shapePunchVibrato,
                    _shapePunchElasticity)
                .SetLink(gameObject);

            ZoomAsync().Forget();
        }

        private void SpawnConfetti()
        {
            StopBurst();

            IReadOnlyList<Vector2Int> fillCells = _gridBuilder.FillCells;

            if (fillCells.Count == 0)
            {
                return;
            }

            Vector3 origin = _gridBuilder.transform.position
                + Vector3.up * (_gridBuilder.Height * _gridBuilder.CellSize * 0.5f);

            _confettiScale = _gridBuilder.CellSize * _confettiScaleFactor;

            for (int i = 0; i < _confettiCount; i++)
            {
                Vector2Int cell = fillCells[Random.Range(0, fillCells.Count)];
                Color color = _gridBuilder.GetPixelColor(cell.x, cell.y);
                FlyingCube confettiCube = _spawner.Spawn(origin, Random.rotation, _confettiScale, color);

                _confetti.Add(confettiCube);
                _velocities.Add(GetRandomVelocity());
                _angularVelocities.Add(GetRandomAngularVelocity());
            }

            _burstElapsedTime = 0f;
            _isBursting = true;
        }

        private Vector3 GetRandomVelocity()
        {
            Vector3 direction = new Vector3(
                Random.Range(-1f, 1f),
                Random.Range(_confettiUpBiasMin, _confettiUpBiasMax),
                Random.Range(-1f, 1f));

            return direction.normalized * Random.Range(0.5f, 1f) * _confettiSpeed;
        }

        private Vector3 GetRandomAngularVelocity()
        {
            return new Vector3(
                Random.Range(-360f, 360f),
                Random.Range(-360f, 360f),
                Random.Range(-360f, 360f));
        }

        private void UpdateConfetti()
        {
            float deltaTime = Time.deltaTime;
            float remainingTime = _confettiDuration - _burstElapsedTime;
            float fadeDuration = _confettiDuration * _confettiFadeFraction;

            for (int i = 0; i < _confetti.Count; i++)
            {
                FlyingCube confettiCube = _confetti[i];

                _velocities[i] += Vector3.down * (_confettiGravity * deltaTime);
                confettiCube.transform.position += _velocities[i] * deltaTime;
                confettiCube.transform.Rotate(_angularVelocities[i] * deltaTime);

                if (fadeDuration > 0f && remainingTime < fadeDuration)
                {
                    confettiCube.transform.localScale = Vector3.one * (_confettiScale * remainingTime / fadeDuration);
                }
            }
        }

        private void StopBurst()
        {
            for (int i = 0; i < _confetti.Count; i++)
            {
                Destroy(_confetti[i].gameObject);
            }

            _confetti.Clear();
            _velocities.Clear();
            _angularVelocities.Clear();
            _isBursting = false;
        }

        private async UniTaskVoid ZoomAsync()
        {
            CancellationToken cancellationToken = this.GetCancellationTokenOnDestroy();
            float elapsedTime = 0f;

            try
            {
                while (elapsedTime < _fovDuration)
                {
                    elapsedTime += Time.deltaTime;
                    float progress = Mathf.Clamp01(elapsedTime / _fovDuration);
                    float easedProgress = 1f - (1f - progress) * (1f - progress);

                    _camera.fieldOfView = _startFov + _fovKick * easedProgress;

                    await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
                }
            }
            catch (OperationCanceledException)
            {
                return;
            }
        }
    }
}
