using System;
using DG.Tweening;
using UnityEngine;

namespace UI.Animations
{
    public class UiScaleAnimator : TweenAnimatorBase
    {
        private const float BoundsFillRatio = 0.92f;

        [SerializeField] private Transform _target;
        [SerializeField] private RectTransform _bounds;
        [SerializeField, Min(0f)] private float _enabledScale = 1f;
        [SerializeField, Min(0f)] private float _disabledScale;

        private Vector3 _baseScale;
        private RectTransform _targetRect;
        private bool _needsScaleRefresh;

        protected override void Awake()
        {
            base.Awake();

            if (_target == null)
            {
                throw new InvalidOperationException($"{name}: assign the scale animation target.");
            }

            _baseScale = _target.localScale;

            if (_bounds != null)
            {
                if (_target.TryGetComponent(out _targetRect) == false)
                {
                    throw new InvalidOperationException($"{name}: fitting to bounds requires a RectTransform target.");
                }

                _needsScaleRefresh = true;
            }
        }

        protected override Tween CreateEnableTween()
        {
            return _target.DOScale(CalculateEnabledScale(), ShowDuration);
        }

        protected override Tween CreateDisableTween()
        {
            return _target.DOScale(_baseScale * _disabledScale, HideDuration);
        }

        protected override void ApplyEnabledState()
        {
            _target.localScale = CalculateEnabledScale();
        }

        protected override void ApplyDisabledState()
        {
            _target.localScale = _baseScale * _disabledScale;
        }

        private Vector3 CalculateEnabledScale()
        {
            Vector3 scale = _baseScale * _enabledScale;

            if (_bounds == null || _targetRect == null)
            {
                return scale;
            }

            float width = _targetRect.rect.width * scale.x;
            float height = _targetRect.rect.height * scale.y;

            if (width <= 0f || height <= 0f || _bounds.rect.width <= 0f || _bounds.rect.height <= 0f)
            {
                return scale;
            }

            float horizontalFit = _bounds.rect.width * BoundsFillRatio / width;
            float verticalFit = _bounds.rect.height * BoundsFillRatio / height;
            float fit = Mathf.Min(1f, Mathf.Min(horizontalFit, verticalFit));
            return scale * fit;
        }

        private void OnRectTransformDimensionsChange()
        {
            _needsScaleRefresh = _bounds != null;
        }

        private void LateUpdate()
        {
            if (_needsScaleRefresh == false || View == null || View.IsActive == false || IsAnimating() == true)
            {
                return;
            }

            _needsScaleRefresh = false;
            ApplyEnabledState();
        }
    }
}
