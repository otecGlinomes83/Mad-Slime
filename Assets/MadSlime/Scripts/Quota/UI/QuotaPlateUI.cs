using Quota;
using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public sealed class QuotaPlateUI : MonoBehaviour
    {
        [SerializeField] private Image _icon;
        [SerializeField] private TMP_Text _text;
        [SerializeField, Range(0f, 0.5f)] private float _popStrength = 0.12f;
        [SerializeField, Min(0.01f)] private float _popDuration = 0.18f;

        private QuotaEntry _entry;
        private Vector3 _baseScale;
        private Tween _popTween;
        private Tween _moveTween;
        private Tween _lifeTween;
        private Action _onRemovalCompleted;

        public QuotaEntry Entry => _entry;

        private void Awake()
        {
            if (_icon == null)
            {
                throw new InvalidOperationException(
                    $"{name}: Icon is not assigned. Drag an Image into the _icon field.");
            }

            if (_text == null)
            {
                throw new InvalidOperationException(
                    $"{name}: Text is not assigned. Drag a TMP_Text into the _text field.");
            }

            _baseScale = transform.localScale;
        }

        private void OnDestroy()
        {
            KillTweens();
        }

        public void Setup(QuotaEntry entry)
        {
            _entry = entry;
            _icon.sprite = entry.Definition.Icon;
        }

        public void PlayIntro(Vector2 startPosition, Vector2 finalPosition, float duration)
        {
            _moveTween?.Kill();

            transform.localPosition = startPosition;

            _moveTween = transform.DOLocalMove(finalPosition, duration)
                .SetEase(Ease.OutCubic)
                .SetUpdate(true)
                .SetLink(gameObject);
        }

        public void UpdateCount(int remaining)
        {
            _text.text = remaining.ToString();
            PlayPop();
        }

        public void MoveTo(Vector3 localPosition, float duration)
        {
            _moveTween?.Kill();

            _moveTween = transform.DOLocalMove(localPosition, duration)
                .SetEase(Ease.OutCubic)
                .SetUpdate(true)
                .SetLink(gameObject);
        }

        public void PlayRemoval(float duration, Action onCompleted)
        {
            KillTweens();

            _onRemovalCompleted = onCompleted;

            _lifeTween = transform.DOScale(Vector3.zero, duration)
                .SetEase(Ease.InBack)
                .SetUpdate(true)
                .SetLink(gameObject)
                .OnComplete(OnRemovalCompleted);
        }

        private void OnRemovalCompleted()
        {
            Action completed = _onRemovalCompleted;
            _onRemovalCompleted = null;
            completed?.Invoke();
            Destroy(gameObject);
        }

        private void PlayPop()
        {
            if (_popTween != null)
            {
                _popTween.Kill();
            }

            float popProgress = 0f;
            transform.localScale = _baseScale;

            _popTween = DOTween.To(
                    () => popProgress,
                    value =>
                    {
                        popProgress = value;
                        transform.localScale = _baseScale * (1f + _popStrength * (1f - value));
                    },
                    1f,
                    _popDuration)
                .SetEase(Ease.OutQuad)
                .SetUpdate(true)
                .SetTarget(this)
                .SetLink(gameObject);
        }

        private void KillTweens()
        {
            if (_popTween != null)
            {
                _popTween.Kill();
                _popTween = null;
            }

            if (_moveTween != null)
            {
                _moveTween.Kill();
                _moveTween = null;
            }

            if (_lifeTween != null)
            {
                _lifeTween.Kill();
                _lifeTween = null;
            }
        }
    }
}
