using System;
using ShapeFill;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace UI
{
    public class FillProgressUI : MonoBehaviour
    {
        [SerializeField] private Image _progressBar;
        [SerializeField] private TMP_Text _percentText;

        private const float FullPercent = 100f;

        private ShapeFiller _shapeFiller;

        [Inject]
        public void Construct(ShapeFiller shapeFiller)
        {
            _shapeFiller = shapeFiller;
        }

        private void Awake()
        {
            if (_shapeFiller == null)
            {
                throw new InvalidOperationException(
                    $"{name}: ShapeFiller was not injected. Check that FillLifetimeScope registers ShapeFiller and FillProgressUI.");
            }

            if (_percentText == null)
            {
                throw new InvalidOperationException($"{name}: assign the fill percentage text.");
            }

            if (_progressBar == null)
            {
                throw new InvalidOperationException($"{name}: assign the fill progress image.");
            }
        }

        private void OnEnable()
        {
            _shapeFiller.CubeArrived += OnCubeArrived;
            _shapeFiller.FillCompleted += OnFillCompleted;
            ApplyProgress(0f);
        }

        private void OnDisable()
        {
            _shapeFiller.CubeArrived -= OnCubeArrived;
            _shapeFiller.FillCompleted -= OnFillCompleted;
        }

        private void OnFillCompleted(float percent)
        {
            ApplyProgress(percent);
        }

        private void OnCubeArrived(CubeFlightAnimator cube)
        {
            ApplyProgress(_shapeFiller.GetFormFillFraction());
        }

        private void ApplyProgress(float fraction)
        {
            float fillAmount = Mathf.Clamp01(fraction);
            _progressBar.fillAmount = fillAmount;
            int percent = Mathf.RoundToInt(fillAmount * FullPercent);
            _percentText.SetText("{0}%", percent);

            if (_progressBar.type == Image.Type.Sliced)
            {
                RectTransform fill = _progressBar.rectTransform;
                fill.anchorMax = new Vector2(fillAmount, 1f);
                fill.offsetMin = Vector2.zero;
                fill.offsetMax = Vector2.zero;
                _progressBar.enabled = fillAmount > 0f;
            }
        }
    }
}
