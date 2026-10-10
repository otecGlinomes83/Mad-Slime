using DG.Tweening;
using System;
using UnityEngine;
using UnityEngine.UI;

namespace Player
{
    public class FinalCountdownVignette : MonoBehaviour
    {
        [SerializeField] private Image _vignetteImage;

        [Tooltip("Длительность одного полупериода пульсации (с).")]
        [SerializeField, Min(0.05f)] private float _pulseDuration = 0.4f;

        [Tooltip("Максимальная яркость мигания.")]
        [SerializeField, Range(0f, 1f)] private float _maxIntensity = 0.8f;

        private Tween _pulseTween;

        private void Awake()
        {
            if (_vignetteImage == null)
            {
                throw new InvalidOperationException(
                    $"{name}: VignetteImage is not assigned. Drag the fullscreen vignette Image into the _vignetteImage field.");
            }

            _vignetteImage.enabled = false;
        }

        private void OnDisable()
        {
            StopPulse();
        }

        public void StartPulse()
        {
            if (_pulseTween != null)
            {
                return;
            }

            _vignetteImage.enabled = true;

            _pulseTween = DOTween.To(ReadIntensity, ApplyIntensity, _maxIntensity, _pulseDuration)
                .SetLoops(-1, LoopType.Yoyo)
                .SetEase(Ease.InOutSine)
                .SetTarget(this)
                .SetLink(gameObject, LinkBehaviour.KillOnDisable);
        }

        public void StopPulse()
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
