using System;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    [DefaultExecutionOrder(-2000), DisallowMultipleComponent]
    public class ResponsiveCanvasLayout : MonoBehaviour
    {
        private const float PortraitWidth = 1080f;
        private const float PortraitHeight = 1920f;

        [Serializable]
        public struct Placement
        {
            public Vector2 AnchorMin;
            public Vector2 AnchorMax;
            public Vector2 Pivot;
            public Vector2 Position;
            public Vector2 Size;
            public float Scale;

            public void Apply(RectTransform target)
            {
                target.anchorMin = AnchorMin;
                target.anchorMax = AnchorMax;
                target.pivot = Pivot;
                target.sizeDelta = Size;
                target.anchoredPosition = Position;
                float scale = 1f;

                if (Scale > 0f)
                {
                    scale = Scale;
                }

                target.localScale = Vector3.one * scale;
            }
        }

        [Serializable]
        public struct Element
        {
            public RectTransform Target;
            public Transform PortraitParent;
            public Transform LandscapeParent;
            public Placement Portrait;
            public Placement Landscape;
        }

        [Serializable]
        public struct Visibility
        {
            public GameObject Target;
            public bool Portrait;
            public bool Landscape;
        }

        [SerializeField] private CanvasScaler _scaler;
        [SerializeField] private Element[] _elements = Array.Empty<Element>();
        [SerializeField] private Visibility[] _visibility = Array.Empty<Visibility>();

        private Vector2Int _screen;

        private void Awake()
        {
            Apply();
        }

        private void OnEnable()
        {
            Apply();
        }

        private void Update()
        {
            if (_screen.x != Screen.width || _screen.y != Screen.height)
            {
                Apply();
            }
        }

        public bool Manages(RectTransform target)
        {
            foreach (Element element in _elements)
            {
                if (element.Target == target)
                {
                    return true;
                }
            }

            return false;
        }

        public void Apply()
        {
            _screen = new Vector2Int(Screen.width, Screen.height);
            ApplyOrientation(_screen.x > _screen.y);
        }

        public void ApplyOrientation(bool landscape)
        {
            if (_scaler != null)
            {
                _scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                _scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
                Vector2 referenceResolution = new Vector2(PortraitWidth, PortraitHeight);

                if (landscape)
                {
                    referenceResolution = new Vector2(PortraitHeight, PortraitWidth);
                }

                _scaler.referenceResolution = referenceResolution;
            }

            foreach (Element element in _elements)
            {
                if (element.Target == null)
                {
                    continue;
                }

                Transform parent = element.PortraitParent;
                Placement placement = element.Portrait;

                if (landscape)
                {
                    parent = element.LandscapeParent;
                    placement = element.Landscape;
                }

                if (parent != null && element.Target.parent != parent)
                {
                    element.Target.SetParent(parent, false);
                    element.Target.SetAsLastSibling();
                }

                placement.Apply(element.Target);
            }

            foreach (Visibility entry in _visibility)
            {
                if (entry.Target == null)
                {
                    continue;
                }

                bool isVisible = entry.Portrait;

                if (landscape)
                {
                    isVisible = entry.Landscape;
                }

                entry.Target.SetActive(isVisible);
            }
        }
    }
}
