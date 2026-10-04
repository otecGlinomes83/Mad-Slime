using DG.Tweening;
using Game;
using System;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace UI
{
    public sealed class FinalCountdownVignette : MonoBehaviour
    {
        [SerializeField] private Image _vignetteImage;

        [Tooltip("Длительность одного полупериода пульсации (с).")]
        [SerializeField, Min(0.05f)] private float _pulseDuration = 0.4f;

        [Tooltip("Максимальная яркость мигания.")]
        [SerializeField, Range(0f, 1f)] private float _maxIntensity = 0.8f;

        private Timer _timer;
        private Tween _pulseTween;

        [Inject]
        public void Construct(Timer timer)
        {
            _timer = timer;
        }

        private void Awake()
        {
            if (_timer == null)
            {
                throw new InvalidOperationException(
                    $"{name}: Timer was not injected. Check that GameLifetimeScope registers Timer and FinalCountdownVignette.");
            }

            if (_vignetteImage == null)
            {
                throw new InvalidOperationException(
                    $"{name}: VignetteImage is not assigned. Drag the fullscreen vignette Image into the _vignetteImage field.");
            }
        }

        private void OnEnable()
        {
            _timer.FinalCountdownStarted += OnFinalCountdownStarted;
            _timer.Finished += OnTimerFinished;
            _timer.Stopped += OnTimerStopped;
        }

        private void OnDisable()
        {
            _timer.FinalCountdownStarted -= OnFinalCountdownStarted;
            _timer.Finished -= OnTimerFinished;
            _timer.Stopped -= OnTimerStopped;

            StopPulse();
        }

        private void OnFinalCountdownStarted()
        {
            _vignetteImage.enabled = true;

            _pulseTween = DOTween.To(ReadIntensity, ApplyIntensity, _maxIntensity, _pulseDuration)
                .SetLoops(-1, LoopType.Yoyo)
                .SetEase(Ease.InOutSine)
                .SetTarget(this);
        }

        private void OnTimerFinished()
        {
            StopPulse();
        }

        private void OnTimerStopped()
        {
            StopPulse();
        }

        private void StopPulse()
        {
            if (_pulseTween != null)
            {
                _pulseTween.Kill();
                _pulseTween = null;
            }

            _vignetteImage.enabled = false;
            ApplyIntensity(0f);
        }

        private float ReadIntensity()
        {
            return _vignetteImage.color.a;
        }

        private void ApplyIntensity(float intensity)
        {
            _vignetteImage.color = new Color(1f, 1f, 1f, intensity);
        }
    }
}
