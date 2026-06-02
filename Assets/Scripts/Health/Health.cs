using System;
using UnityEngine;

namespace Health
{
    public sealed class Health : MonoBehaviour
    {
        [SerializeField] private int _maxValue;
        [SerializeField] private int _value;

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
        }

        public void TakeDamage(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            if (_value <= 0)
            {
                return;
            }

            _value = Mathf.Max(0, _value - amount);

            Damaged?.Invoke(amount);
            ValueChanged?.Invoke(_value);

            if (_value <= 0)
            {
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

            ValueChanged?.Invoke(_value);
        }
    }
}
