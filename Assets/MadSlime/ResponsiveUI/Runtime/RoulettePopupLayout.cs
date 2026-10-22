using System;
using UnityEngine;

namespace UI
{
    [DisallowMultipleComponent, RequireComponent(typeof(ResponsiveCanvasLayout))]
    public class RoulettePopupLayout : MonoBehaviour
    {
        [SerializeField] private ResponsiveCanvasLayout.Element[] _withoutControls = Array.Empty<ResponsiveCanvasLayout.Element>();

        [SerializeField] private RectTransform _spinButton;
        [SerializeField] private RectTransform _adButton;

        private bool _isSpinVisible;
        private bool _isAdVisible;
        private Vector2Int _screen;
        private ResponsiveCanvasLayout _profiles;

        public void SetControlsVisible(bool showSpin, bool showAd)
        {
            _isSpinVisible = showSpin;
            _isAdVisible = showAd;
            Apply();
        }

        private void Update()
        {
            if (_screen.x != Screen.width || _screen.y != Screen.height)
            {
                Apply();
            }
        }

        private void Apply()
        {
            _screen = new Vector2Int(Screen.width, Screen.height);
            EnsureProfiles();
            ApplyOrientation(_screen.x > _screen.y, _isSpinVisible == false && _isAdVisible == false);
            CenterSingleControl();
        }

        public void ApplyOrientation(bool landscape, bool withoutControls)
        {
            EnsureProfiles();
            _profiles.ApplyOrientation(landscape);

            if (withoutControls == false)
            {
                return;
            }

            foreach (ResponsiveCanvasLayout.Element element in _withoutControls)
            {
                if (element.Target == null)
                {
                    continue;
                }

                ResponsiveCanvasLayout.Placement placement = element.Portrait;

                if (landscape)
                {
                    placement = element.Landscape;
                }

                placement.Apply(element.Target);
            }
        }

        private void EnsureProfiles()
        {
            if (_profiles != null)
            {
                return;
            }

            if (TryGetComponent(out _profiles) == false)
            {
                throw new InvalidOperationException($"{name}: ResponsiveCanvasLayout is required.");
            }
        }

        private void CenterSingleControl()
        {
            if (_isSpinVisible && _isAdVisible == false && _spinButton != null)
            {
                _spinButton.anchoredPosition = new Vector2(0f, _spinButton.anchoredPosition.y);
            }

            if (_isAdVisible && _isSpinVisible == false && _adButton != null)
            {
                _adButton.anchoredPosition = new Vector2(0f, _adButton.anchoredPosition.y);
            }
        }
    }
}
