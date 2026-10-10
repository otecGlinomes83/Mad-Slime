using System;
using UI.Animations;
using UnityEngine;

namespace UI
{
    [RequireComponent(typeof(UiScaleAnimator))]
    public class BaseWindow : MonoBehaviour, IShowable
    {
        public event Action Closed;

        public event Action CloseRequested;

        private UiScaleAnimator _scaleAnimator;
        private bool _isClosing;
        private bool _isCloseRequested;
        private bool _isClosed;

        protected bool IsClosing => _isClosing;

        protected virtual void Awake()
        {
            if (TryGetComponent(out _scaleAnimator) == false)
            {
                throw new InvalidOperationException(
                    $"{name}: UiScaleAnimator is missing. The window prefab must contain a UiScaleAnimator component.");
            }
        }

        public virtual void Show()
        {
            _scaleAnimator.HideCompleted -= OnHideCompleted;
            _isClosing = false;
            _isCloseRequested = false;
            _isClosed = false;
            gameObject.SetActive(true);
            _scaleAnimator.PlayShow();
        }

        public void Hide()
        {
            BeginClose();
        }

        public void RequestClose()
        {
            if (_isClosing == true || _isCloseRequested == true)
            {
                return;
            }

            _isCloseRequested = true;
            Action closeRequested = CloseRequested;
            closeRequested?.Invoke();
        }

        public void BeginClose()
        {
            if (_isClosing == true)
            {
                return;
            }

            _isClosing = true;
            OnClosing();

            _scaleAnimator.HideCompleted += OnHideCompleted;
            _scaleAnimator.PlayHide();
        }

        protected virtual void OnClosing()
        {
        }

        protected virtual void OnDisable()
        {
            if (_scaleAnimator != null)
            {
                _scaleAnimator.HideCompleted -= OnHideCompleted;
            }
        }

        private void OnHideCompleted()
        {
            if (_isClosed == true)
            {
                return;
            }

            _isClosed = true;
            _scaleAnimator.HideCompleted -= OnHideCompleted;

            Action closed = Closed;
            closed?.Invoke();
        }
    }
}
