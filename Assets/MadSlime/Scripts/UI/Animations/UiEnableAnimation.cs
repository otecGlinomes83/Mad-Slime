using System;
using DG.Tweening;
using UnityEngine;

namespace UI.Animations
{
    public sealed class UiEnableAnimation : MonoBehaviour, IShowable
    {
        [SerializeField] private UiAppearMode _mode = UiAppearMode.Scale;

        [SerializeField, Min(0.01f)] private float _duration = 0.3f;

        [SerializeField, Min(0f)] private float _slideOffset = 140f;

        private RectTransform _rect;
        private Vector2 _enabledPosition;
        private Vector3 _enabledScale;
        private Tween _currentTween;

        public event Action Closed;

        private void Awake()
        {
            _rect = transform as RectTransform;

            if (_mode != UiAppearMode.Scale && _rect == null)
            {
                throw new InvalidOperationException(
                    $"{name}: UiAppearMode.{_mode} requires a RectTransform on the same GameObject. Use the Scale mode for plain Transforms.");
            }

            CaptureEnabledState();
        }

        private void OnEnable()
        {
            if (_mode == UiAppearMode.None)
            {
                return;
            }

            PlayIntro();
        }

        private void OnDisable()
        {
            KillCurrentTween();
        }

        public void Show()
        {
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            PlayOutro();
        }

        public void PlayOutro()
        {
            if (_mode == UiAppearMode.None)
            {
                gameObject.SetActive(false);

                Action closed = Closed;
                closed?.Invoke();
                return;
            }

            KillCurrentTween();

            if (_mode == UiAppearMode.Scale)
            {
                _currentTween = transform.DOScale(Vector3.zero, _duration)
                    .SetEase(Ease.InBack);
            }
            else
            {
                _currentTween = CreateMoveTween(OutPositionFor(_mode), _duration, Ease.InCubic);
            }

            _currentTween
                .SetUpdate(true)
                .SetLink(gameObject)
                .OnComplete(OnOutroCompleted);
        }

        private void PlayIntro()
        {
            KillCurrentTween();

            if (_mode == UiAppearMode.Scale)
            {
                transform.localScale = Vector3.zero;

                _currentTween = transform.DOScale(_enabledScale, _duration)
                    .SetEase(Ease.OutBack)
                    .SetUpdate(true);
            }
            else
            {
                _rect.anchoredPosition = OutPositionFor(_mode);

                _currentTween = CreateMoveTween(_enabledPosition, _duration, Ease.OutCubic);
            }

            _currentTween
                .SetUpdate(true)
                .SetLink(gameObject)
                .OnComplete(OnIntroCompleted);
        }

        private void OnIntroCompleted()
        {
            _currentTween = null;
            ApplyEnabledState();
        }

        private void OnOutroCompleted()
        {
            _currentTween = null;
            ApplyEnabledState();
            gameObject.SetActive(false);

            Action closed = Closed;
            closed?.Invoke();
        }

        private void ApplyEnabledState()
        {
            if (_rect != null)
            {
                _rect.anchoredPosition = _enabledPosition;
            }

            transform.localScale = _enabledScale;
        }

        private Tween CreateMoveTween(Vector2 to, float duration, Ease ease)
        {
            return DOTween.To(ReadAnchoredPosition, ApplyAnchoredPosition, to, duration)
                .SetEase(ease);
        }

        private Vector2 ReadAnchoredPosition()
        {
            return _rect.anchoredPosition;
        }

        private void ApplyAnchoredPosition(Vector2 position)
        {
            _rect.anchoredPosition = position;
        }

        private Vector2 OutPositionFor(UiAppearMode mode)
        {
            switch (mode)
            {
                case UiAppearMode.FromLeft:
                    return _enabledPosition + new Vector2(-_slideOffset, 0f);

                case UiAppearMode.FromRight:
                    return _enabledPosition + new Vector2(_slideOffset, 0f);

                case UiAppearMode.FromTop:
                    return _enabledPosition + new Vector2(0f, _slideOffset);

                case UiAppearMode.FromBottom:
                    return _enabledPosition + new Vector2(0f, -_slideOffset);

                default:
                    return _enabledPosition;
            }
        }

        private void CaptureEnabledState()
        {
            if (_rect != null)
            {
                _enabledPosition = _rect.anchoredPosition;
            }

            _enabledScale = transform.localScale;
        }

        private void KillCurrentTween()
        {
            if (_currentTween == null)
            {
                return;
            }

            _currentTween.Kill();
            _currentTween = null;
        }
    }
}
