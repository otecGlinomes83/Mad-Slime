using UnityEngine;
using UnityEngine.UI;

namespace UI.Editor
{
    internal class PrefabCanvasSurface
    {
        private Canvas _canvas;
        private RenderMode _renderMode;
        private CanvasScaler _scaler;
        private bool _isScalerEnabled;
        private Vector2 _referenceResolution;
        private CanvasScaler.ScaleMode _scaleMode;
        private CanvasScaler.ScreenMatchMode _matchMode;
        private Vector2 _anchorMin;
        private Vector2 _anchorMax;
        private Vector2 _pivot;
        private Vector2 _size;
        private Vector2 _position;
        private Vector3 _scale;

        public Canvas Canvas => _canvas;

        public PrefabCanvasSurface(Canvas canvas)
        {
            _canvas = canvas;
            _renderMode = canvas.renderMode;

            if (canvas.TryGetComponent(out _scaler))
            {
                _isScalerEnabled = _scaler.enabled;
                _referenceResolution = _scaler.referenceResolution;
                _scaleMode = _scaler.uiScaleMode;
                _matchMode = _scaler.screenMatchMode;
            }

            RectTransform rect = (RectTransform)canvas.transform;
            _anchorMin = rect.anchorMin;
            _anchorMax = rect.anchorMax;
            _pivot = rect.pivot;
            _size = rect.sizeDelta;
            _position = rect.anchoredPosition;
            _scale = rect.localScale;
        }

        public void Show(Vector2 size)
        {
            if (_canvas == null)
            {
                return;
            }

            if (_scaler != null)
            {
                _scaler.enabled = false;
            }

            _canvas.renderMode = RenderMode.WorldSpace;
            RectTransform rect = (RectTransform)_canvas.transform;
            rect.anchorMin = Vector2.one * 0.5f;
            rect.anchorMax = Vector2.one * 0.5f;
            rect.pivot = Vector2.one * 0.5f;
            rect.sizeDelta = size;
            rect.anchoredPosition = Vector2.zero;
            rect.localScale = Vector3.one;
        }

        public void Restore()
        {
            if (_canvas == null)
            {
                return;
            }

            _canvas.renderMode = _renderMode;
            RectTransform rect = (RectTransform)_canvas.transform;
            rect.anchorMin = _anchorMin;
            rect.anchorMax = _anchorMax;
            rect.pivot = _pivot;
            rect.sizeDelta = _size;
            rect.anchoredPosition = _position;
            rect.localScale = _scale;

            if (_scaler == null)
            {
                return;
            }

            _scaler.referenceResolution = _referenceResolution;
            _scaler.uiScaleMode = _scaleMode;
            _scaler.screenMatchMode = _matchMode;
            _scaler.enabled = _isScalerEnabled;
        }
    }
}
