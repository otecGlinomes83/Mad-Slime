using DG.Tweening;
using ShapeFill;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace UI
{
    public sealed class FillProgressUI : MonoBehaviour
    {
        [SerializeField] private ShapeFiller _shapeFiller;
        [SerializeField] private TMP_Text _percentText;
        [SerializeField, Range(0f, 1f)] private float _punchStrength = 0.25f;
        [SerializeField, Min(0.01f)] private float _punchDuration = 0.25f;
        [SerializeField, Min(0)] private int _punchVibrato = 8;
        [SerializeField, Range(0f, 1f)] private float _punchElasticity = 0.4f;
        [SerializeField] private List<int> _punchMilestones = new List<int> { 25, 50, 75, 100 };

        private int _lastPercent;

        private void Awake()
        {
            if (_shapeFiller == null)
            {
                throw new InvalidOperationException(
                    $"{name}: ShapeFiller is not assigned. Drag a ShapeFiller component into the _shapeFiller field.");
            }

            if (_percentText == null)
            {
                throw new InvalidOperationException(
                    $"{name}: PercentText is not assigned. Drag a TMP_Text into the _percentText field.");
            }

            _percentText.text = "0%";
        }

        private void OnEnable()
        {
            _shapeFiller.CubeArrived += OnCubeArrived;
            _shapeFiller.FillCompleted += OnFillCompleted;
        }

        private void OnDisable()
        {
            _shapeFiller.CubeArrived -= OnCubeArrived;
            _shapeFiller.FillCompleted -= OnFillCompleted;
        }

        private void OnFillCompleted(float percent)
        {
            gameObject.SetActive(false);
        }

        private void OnCubeArrived(FlyingCube cube)
        {
            int percent = Mathf.RoundToInt(_shapeFiller.FillFraction * 100f);

            if (percent == _lastPercent)
            {
                return;
            }

            _lastPercent = percent;
            _percentText.text = $"{percent}%";

            if (_punchMilestones.Contains(percent) == true)
            {
                Punch();
            }
        }

        private void Punch()
        {
            _percentText.transform.DOKill();
            _percentText.transform.DOPunchScale(
                    Vector3.one * _punchStrength,
                    _punchDuration,
                    _punchVibrato,
                    _punchElasticity)
                .SetLink(gameObject);
        }
    }
}
