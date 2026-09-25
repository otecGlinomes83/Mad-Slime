using DG.Tweening;
using Scriptables;
using System;
using System.Collections.Generic;
using UnityEngine;
using VContainer;
using Random = UnityEngine.Random;

namespace ShapeFill
{
    public sealed class FillFinale : MonoBehaviour
    {
        [SerializeField] private ParticleSystem _confetti;

        private FillConfig _config;
        private ShapeFillOrchestrator _orchestrator;
        private GridBuilder _gridBuilder;
        private int _confettiCount;
        private Camera _camera;
        private float _startFov;

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
                    $"{name}: Confetti particle system is not assigned. Drag the confetti ParticleSystem into the _confetti field.");
            }

            ParticleSystem.EmissionModule emission = _confetti.emission;

            if (emission.burstCount == 0)
            {
                throw new InvalidOperationException(
                    $"{name}: Confetti burst is not set. Set Emission → Bursts on the particle system: its Count is the confetti amount.");
            }

            _confettiCount = Mathf.CeilToInt(emission.GetBurst(0).count.constant);

            if (_confettiCount <= 0)
            {
                throw new InvalidOperationException(
                    $"{name}: Confetti burst count is {_confettiCount}. Set a positive amount in Emission → Bursts.");
            }

            emission.SetBursts(Array.Empty<ParticleSystem.Burst>());
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

            _camera = mainCamera;
            _startFov = _camera.fieldOfView;

            EmitConfetti();
            PlayShapePunch();
            PlayCameraKick();
        }

        private void EmitConfetti()
        {
            IReadOnlyList<Vector2Int> fillCells = _gridBuilder.FillCells;

            if (fillCells.Count == 0)
            {
                return;
            }

            for (int i = 0; i < _confettiCount; i++)
            {
                Vector2Int cell = fillCells[Random.Range(0, fillCells.Count)];

                ParticleSystem.EmitParams parameters = new ParticleSystem.EmitParams
                {
                    startColor = _gridBuilder.GetPixelColor(cell.x, cell.y)
                };

                _confetti.Emit(parameters, 1);
            }
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

        private void PlayCameraKick()
        {
            _camera.DOKill();

            Sequence kick = DOTween.Sequence().SetLink(gameObject);
            kick.Append(_camera.DOFieldOfView(_startFov + _config.FovKick, _config.FovDuration).SetEase(Ease.OutQuad));
            kick.Append(_camera.DOFieldOfView(_startFov, _config.FovDuration).SetEase(Ease.InQuad));
        }
    }
}
