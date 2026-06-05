using System;
using Game;
using TMPro;
using UnityEngine;

namespace Health
{
    public sealed class Health : MonoBehaviour
    {
        [SerializeField] private int _maxValue;
        [SerializeField] private int _value;
        [SerializeField] private Timer _timer;
        [SerializeField] private float _invulnerabilityWindow = 0.75f;

        [SerializeField] TMP_Text _healthText;

        private bool _isInvulnerable;

        public event Action<int> Damaged;
        public event Action Died;
        public event Action<int> ValueChanged;

        public int Value => _value;
        public int MaxValue => _maxValue;
        public bool IsAlive => _value > 0;

        private void Awake()
        {
            if (_maxValue <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(_maxValue), "Max health must be greater than zero");
            }

            if (_value < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(_value), "Health value cannot be negative");
            }

            _value = Mathf.Min(_value, _maxValue);
            _isInvulnerable = false;

            if (_timer == null)
            {
                throw new InvalidOperationException(
                    $"{name}: Timer is not assigned. Drag a Timer component into the _timer field in the inspector.");
            }
        }

        private void Start()
        {
            ValueChanged?.Invoke(_value);
        }

        private void OnEnable()
        {
            _timer.Finished += OnInvulnerabilityEnded;
        }

            if (_value < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(_value), "Health value cannot be negative");
            }

            if (_timer == null)
            {
                throw new InvalidOperationException(
                    $"{name}: Timer is not assigned. Drag a Timer component into the _timer field in the inspector.");
            }

            _value = _maxValue;
            _healthText.text = $"{_value}/ {_maxValue}";

            _isInvulnerable = false;
        }

        private void OnEnable()
        {
            _timer.Finished += OnInvulnerabilityEnded;
        }

        private void OnDisable()
        {
            _timer.Finished -= OnInvulnerabilityEnded;
        }

        public void TryApplyDamage(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            if (_value <= 0)
            {
                return;
            }

            if (_isInvulnerable)
            {
                Debug.Log("Health is INVULNERABLE");
                return;
            }

            _isInvulnerable = true;
            _timer.Setup(_invulnerabilityWindow);
            _timer.StartCount();

            _value = Mathf.Max(0, _value - amount);
            _healthText.text = $"{_value}/ {_maxValue}";

            Damaged?.Invoke(amount);
            ValueChanged?.Invoke(_value);

            if (_value <= 0)
            {
                Debug.Log($"{name}: Health {_value} has been die");
                Died?.Invoke();
            }
        }

        public void Heal(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            if (_value <= 0)
            {
                return;
            }

            _value = Mathf.Min(_maxValue, _value + amount);
            _healthText.text = $"{_value}/ {_maxValue}";

            ValueChanged?.Invoke(_value);
        }

        private void OnInvulnerabilityEnded()
        {
            _isInvulnerable = false;
        }
    }
}