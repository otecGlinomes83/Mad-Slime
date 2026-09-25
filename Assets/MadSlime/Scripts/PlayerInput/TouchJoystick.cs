using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.OnScreen;

namespace PlayerInput
{
    public sealed class TouchJoystick : OnScreenControl, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        [InputControl(layout = "Vector2")]
        [SerializeField] private string _controlPath = "<Gamepad>/leftStick";

        [SerializeField] private Canvas _canvas;
        [SerializeField] private RectTransform _background;
        [SerializeField] private RectTransform _handle;

        [SerializeField] private float _movementRange = 120f;

        private RectTransform _zone;
        private int _activePointerId;
        private bool _isDragging;

        private void Awake()
        {
            if (_background == null)
            {
                throw new InvalidOperationException(
                    $"{name}: Background is not assigned. Drag a Background RectTransform into the _background field.");
            }

            if (_handle == null)
            {
                throw new InvalidOperationException(
                    $"{name}: Handle is not assigned. Drag a Handle RectTransform into the _handle field.");
            }

            if (transform is RectTransform == false)
            {
                throw new InvalidOperationException(
                    $"{name}: TouchJoystick must be attached to a UI element with a RectTransform.");
            }

            if (_canvas == null)
            {
                throw new InvalidOperationException(
                    $"{name}: Canvas is not assigned. Drag the Canvas that contains the joystick into the _canvas field.");
            }

            if (_movementRange <= 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(_movementRange), _movementRange, $"{name}: MovementRange must be positive.");
            }

            _zone = (RectTransform)transform;

            HideVisuals();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (_isDragging == true)
            {
                return;
            }

            if (IsTouchAvailable() == false)
            {
                return;
            }

            if (TryGetLocalPoint(eventData, out Vector2 localPoint) == false)
            {
                return;
            }

            _isDragging = true;
            _activePointerId = eventData.pointerId;

            _background.anchoredPosition = localPoint;
            _handle.anchoredPosition = Vector2.zero;

            ShowVisuals();
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (_isDragging == false || eventData.pointerId != _activePointerId)
            {
                return;
            }

            if (TryGetLocalPoint(eventData, out Vector2 localPoint) == false)
            {
                return;
            }

            Vector2 delta = localPoint - _background.anchoredPosition;
            Vector2 clampedDelta = Vector2.ClampMagnitude(delta, _movementRange);

            _handle.anchoredPosition = clampedDelta;

            SendValueToControl(clampedDelta / _movementRange);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (_isDragging == false || eventData.pointerId != _activePointerId)
            {
                return;
            }

            EndDrag();
        }

        protected override void OnDisable()
        {
            EndDrag();

            base.OnDisable();
        }

        protected override string controlPathInternal
        {
            get
            {
                return _controlPath;
            }

            set
            {
                _controlPath = value;
            }
        }

        private static bool IsTouchAvailable()
        {
            if (Application.isEditor == true)
            {
                return true;
            }

            return Touchscreen.current != null;
        }

        private bool TryGetLocalPoint(PointerEventData eventData, out Vector2 localPoint)
        {
            return RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _zone, eventData.position, eventData.pressEventCamera, out localPoint);
        }

        private void ShowVisuals()
        {
            _background.gameObject.SetActive(true);
        }

        private void HideVisuals()
        {
            _background.gameObject.SetActive(false);
        }

        private void EndDrag()
        {
            if (_isDragging == false)
            {
                return;
            }

            _isDragging = false;

            SendValueToControl(Vector2.zero);

            HideVisuals();
        }
    }
}
