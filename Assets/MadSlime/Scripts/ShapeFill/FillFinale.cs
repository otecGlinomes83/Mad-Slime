using Cysharp.Threading.Tasks;
using DG.Tweening;
using Scriptables;
using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using VContainer;

namespace ShapeFill
{
    public sealed class FillFinale : MonoBehaviour
    {
        [Tooltip("Префаб конфетти (ConfettiBlastRainbow из Epic Toon FX). Корневая система ассета зациклена — после первого прохода эмиссия гасится, выпавшие конфетти долетают сами.")]
        [SerializeField] private ParticleSystem _confetti;

        private FillConfig _config;
        private ShapeFillOrchestrator _orchestrator;
        private GridBuilder _gridBuilder;

        [Inject]
        public void Construct(ShapeFillOrchestrator orchestrator, GridBuilder gridBuilder, FillConfig config)
        {
            _orchestrator = orchestrator;
            _gridBuilder = gridBuilder;
            _config = config;
        }

        private void Awake()
        {
            if (_orchestrator == null)
            {
                throw new InvalidOperationException(
                    $"{name}: Orchestrator was not injected. Check that FillLifetimeScope registers ShapeFillOrchestrator and FillFinale.");
            }

            if (_gridBuilder == null)
            {
                throw new InvalidOperationException(
                    $"{name}: GridBuilder was not injected. Check that FillLifetimeScope registers GridBuilder and FillFinale.");
            }

            if (_config == null)
            {
                throw new InvalidOperationException(
                    $"{name}: FillConfig was not injected. Check that FillLifetimeScope has the FillConfig assigned.");
            }

            if (_confetti == null)
            {
                throw new InvalidOperationException(
                    $"{name}: Confetti prefab is not assigned. Drag the ConfettiBlastRainbow prefab (Epic Toon FX) into the _confetti field.");
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

            PlayConfettiAsync().Forget();
            PlayShapePunch();
            PlayCameraKick(mainCamera);
        }

        private async UniTaskVoid PlayConfettiAsync()
        {
            CancellationToken cancellationToken = this.GetCancellationTokenOnDestroy();
            ParticleSystem burst = Instantiate(_confetti, GetShapeCenter(), Quaternion.identity);

            try
            {
                float duration = 0f;
                float maxLifetime = 0f;

                foreach (ParticleSystem system in burst.GetComponentsInChildren<ParticleSystem>(true))
                {
                    ParticleSystem.MainModule main = system.main;
                    duration = Mathf.Max(duration, main.duration);
                    maxLifetime = Mathf.Max(maxLifetime, main.startLifetime.constantMax);

                    if (system.isPlaying == false)
                    {
                        system.Play();
                    }
                }

                await UniTask.Delay(TimeSpan.FromSeconds(duration), cancellationToken: cancellationToken);

                if (burst == null)
                {
                    return;
                }

                burst.Stop(true, ParticleSystemStopBehavior.StopEmitting);

                await UniTask.Delay(TimeSpan.FromSeconds(maxLifetime), cancellationToken: cancellationToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            if (burst != null)
            {
                Destroy(burst.gameObject);
            }
        }

        private Vector3 GetShapeCenter()
        {
            IReadOnlyList<Vector2Int> fillCells = _gridBuilder.FillCells;

            if (fillCells.Count == 0)
            {
                return _gridBuilder.transform.position;
            }

            Vector2 sum = Vector2.zero;

            for (int i = 0; i < fillCells.Count; i++)
            {
                sum += fillCells[i];
            }

            Vector2 center = sum / fillCells.Count;

            return _gridBuilder.GridToWorld(Mathf.RoundToInt(center.x), Mathf.RoundToInt(center.y));
        }

        private void PlayShapePunch()
        {
            _gridBuilder.transform.DOKill();
            _gridBuilder.transform.DOPunchScale(
                    Vector3.one * _config.ShapePunchStrength,
                    _config.ShapePunchDuration,
                    _config.ShapePunchVibrato,
                    _config.ShapePunchElasticity)
                .SetLink(gameObject);
        }

        private void PlayCameraKick(Camera camera)
        {
            float startFov = camera.fieldOfView;

            camera.DOKill();

            Sequence kick = DOTween.Sequence().SetLink(gameObject);
            kick.Append(camera.DOFieldOfView(startFov + _config.FovKick, _config.FovDuration).SetEase(Ease.OutQuad));
            kick.Append(camera.DOFieldOfView(startFov, _config.FovDuration).SetEase(Ease.InQuad));
        }
    }
}
