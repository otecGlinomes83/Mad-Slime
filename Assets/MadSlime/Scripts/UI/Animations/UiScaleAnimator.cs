using System;
using DG.Tweening;
using UnityEngine;

namespace UI.Animations
{
    public class UiScaleAnimator : MonoBehaviour
    {
        [Tooltip("Длительность показа окна (с).")]
        [SerializeField, Min(0.01f)] private float _showDuration = 0.25f;

        [Tooltip("Длительность скрытия окна (с).")]
        [SerializeField, Min(0.01f)] private float _hideDuration = 0.2f;

        [Tooltip("Масштаб, с которого окно выезжает при показе.")]
        [SerializeField, Range(0.01f, 1f)] private float _showFromScale = 0.6f;

        [Tooltip("Изинг выезда.")]
        [SerializeField] private Ease _showEase = Ease.OutBack;

        [Tooltip("Изинг ухода.")]
        [SerializeField] private Ease _hideEase = Ease.InBack;

        private Vector3 _baseScale;
        private Tween _currentTween;

        public event Action ShowCompleted;

        public event Action HideCompleted;

        private void Awake()
        {
            _baseScale = transform.localScale;
        }

        private void OnDisable()
        {
            KillTween();

            transform.localScale = _baseScale;
        }

        public void PlayShow()
        {
            KillTween();

            transform.localScale = _baseScale * _showFromScale;

            _currentTween = transform.DOScale(_baseScale, _showDuration)
                .SetEase(_showEase)
                .SetUpdate(true)
                .SetLink(gameObject)
                .OnComplete(OnShowCompleted);
        }

        public void PlayHide()
        {
            KillTween();

            _currentTween = transform.DOScale(Vector3.zero, _hideDuration)
                .SetEase(_hideEase)
                .SetUpdate(true)
                .SetLink(gameObject)
                .OnComplete(OnHideCompleted);
        }

        private void KillTween()
        {
            if (_currentTween == null)
            {
                return;
            }

            _currentTween.Kill();
            _currentTween = null;
        }

        private void OnShowCompleted()
        {
            _currentTween = null;

            ShowCompleted?.Invoke();
        }

        private void OnHideCompleted()
        {
            _currentTween = null;

            HideCompleted?.Invoke();
        }
    }
}
