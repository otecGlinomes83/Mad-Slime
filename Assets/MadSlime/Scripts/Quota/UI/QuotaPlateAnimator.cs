using System;
using DG.Tweening;
using UnityEngine;

namespace Quota
{
    public class QuotaPlateAnimator : MonoBehaviour
    {
        [SerializeField, Range(0f, 0.5f)] private float _popStrength = 0.12f;
        [SerializeField, Min(0.01f)] private float _popDuration = 0.18f;

        private Vector3 _baseScale;
        private Tween _popTween;
        private Tween _moveTween;
        private Sequence _removalTween;

        public event Action<QuotaPlateAnimator> RemovalCompleted;

        private void Awake()
        {
            _baseScale = transform.localScale;
        }

        private void OnDisable()
        {
            Cancel();
        }

        private void OnDestroy()
        {
            Cancel();
        }

        public Tween CreateIntro(Vector3 startPosition, Vector3 finalPosition, float duration)
        {
            Cancel();
            transform.localPosition = startPosition;
            _moveTween = transform.DOLocalMove(finalPosition, duration)
                .SetEase(Ease.OutCubic);
            return _moveTween;
        }

        public void MoveTo(Vector3 localPosition, float duration)
        {
            _moveTween?.Kill();
            _moveTween = transform.DOLocalMove(localPosition, duration)
                .SetEase(Ease.OutCubic)
                .SetUpdate(true)
                .SetLink(gameObject);
        }

        public void PlayPop()
        {
            _popTween?.Kill();
            transform.localScale = _baseScale * (1f + _popStrength);
            _popTween = transform.DOScale(_baseScale, _popDuration)
                .SetEase(Ease.OutQuad)
                .SetUpdate(true)
                .SetLink(gameObject);
        }

        public void PlayRemoval(float duration, float slideOffset)
        {
            Cancel();
            Vector3 destination = transform.localPosition + Vector3.left * slideOffset;
            _removalTween = DOTween.Sequence();
            _removalTween.Join(transform.DOLocalMove(destination, duration).SetEase(Ease.InCubic));
            _removalTween.Join(transform.DOScale(Vector3.zero, duration).SetEase(Ease.InBack));
            _removalTween.SetUpdate(true).SetLink(gameObject).OnComplete(OnRemovalCompleted);
        }

        public void Cancel()
        {
            _popTween?.Kill();
            _moveTween?.Kill();
            _removalTween?.Kill();
            _popTween = null;
            _moveTween = null;
            _removalTween = null;
            transform.localScale = _baseScale;
        }

        private void OnRemovalCompleted()
        {
            _removalTween = null;
            RemovalCompleted?.Invoke(this);
        }
    }
}
