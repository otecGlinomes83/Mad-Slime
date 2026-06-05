using System;
using Health;
using TMPro;
using UnityEngine;

namespace UI
{
    public sealed class HealthUI : MonoBehaviour
    {
        [SerializeField] private Health _health;
        [SerializeField] private TMP_Text _valueText;

        private void Awake()
        {
            if (_health == null)
            {
                throw new InvalidOperationException(
                    $"{name}: Health is not assigned. Drag a Health component into the _health field.");
            }

            if (_valueText == null)
            {
                throw new InvalidOperationException(
                    $"{name}: TMP_Text is not assigned. Drag a TMP_Text component into the _valueText field.");
            }
        }

        private void OnEnable()
        {
            _health.ValueChanged += OnHealthValueChanged;
            _health.Died += OnHealthDied;
        }

        private void OnDisable()
        {
            _health.ValueChanged -= OnHealthValueChanged;
            _health.Died -= OnHealthDied;
        }

        private void OnHealthValueChanged(int currentValue)
        {
            _valueText.text = $"{currentValue} / {_health.MaxValue}";
        }

        private void OnHealthDied()
        {
            _valueText.text = $"0 / {_health.MaxValue}";
        }
    }
}
