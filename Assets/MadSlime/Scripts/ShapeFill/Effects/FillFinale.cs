using Audio;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Scriptables;
using System;
using System.Threading;
using UnityEngine;
using VContainer;

namespace ShapeFill
{
    public sealed class FillFinale : MonoBehaviour
    {
        [Tooltip("Система конфетти на сцене Fill (не префаб-ассет): настраивается в редакторе. На Awake гасится, играет только по FillCompleted.")]
        [SerializeField] private ParticleSystem _confetti;

        private FillConfig _config;
        private ShapeFillOrchestrator _orchestrator;
        private GridBuilder _gridBuilder;
        private SfxPlayer _sfxPlayer;

        [Inject]
        public void Construct(ShapeFillOrchestrator orchestrator, GridBuilder gridBuilder, FillConfig config,
            SfxPlayer sfxPlayer)
        {
            _orchestrator = orchestrator;
            _gridBuilder = gridBuilder;
            _config = config;
            _sfxPlayer = sfxPlayer;
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

            if (_sfxPlayer == null)
            {
                throw new InvalidOperationException(
                    $"{name}: SfxPlayer was not injected. Check that ProjectLifetimeScope registers SfxPlayer.");
            }

            if (_confetti == null)
            {
                throw new InvalidOperationException(
                    $"{name}: Confetti is not assigned. Place the ConfettiBlastRainbow prefab into the Fill scene and drag the instance into the _confetti field.");
            }

            if (_confetti.gameObject.scene.IsValid() == false)
            {
                throw new InvalidOperationException(
                    $"{name}: Confetti must be a scene instance, not a prefab asset. Drag the scene object into the _confetti field.");
            }

            _confetti.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
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

            try
            {
                float duration = 0f;
                float maxLifetime = 0f;

                foreach (ParticleSystem system in _confetti.GetComponentsInChildren<ParticleSystem>(true))
                {
                    ParticleSystem.MainModule main = system.main;
                    duration = Mathf.Max(duration, main.duration);
                    maxLifetime = Mathf.Max(maxLifetime, main.startLifetime.constantMax);
                }

                PlayFinaleClip();
                _confetti.Play(true);

                await UniTask.Delay(TimeSpan.FromSeconds(duration), cancellationToken: cancellationToken);

                _confetti.Stop(true, ParticleSystemStopBehavior.StopEmitting);

                await UniTask.Delay(TimeSpan.FromSeconds(maxLifetime), cancellationToken: cancellationToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }
        }

        private void PlayFinaleClip()
        {
            if (_config.FinaleClip == null || _sfxPlayer == null)
            {
                return;
            }

            _sfxPlayer.PlayGame(_config.FinaleClip);
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
