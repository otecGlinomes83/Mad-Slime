using System;
using Game;
using UnityEngine;

namespace Assets.Scripts.HealthSystem
{
    public sealed class Health : MonoBehaviour
    {
        [SerializeField] private int _maxValue;
        [SerializeField] private int _value;
        [SerializeField] private Timer _timer;
        [SerializeField] private float _invulnerabilityWindow = 0.5f;

        private bool _isInvulnerable;

        public event Action Died;
        public event Action Damaged;
        public event Action<int> ValueChanged;
        public event Action InvulnerabilityEnded;

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
                throw new ArgumentOutOfRangeException(nameof(_value), "HealthSystem value cannot be negative");
            }

            _value = _maxValue;
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
            _timer.Finished += OnIFramesTimerFinished;
        }

        private void OnDisable()
        {
            _timer.Finished -= OnIFramesTimerFinished;
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

            if (_isInvulnerable == true)
            {
                return;
            }

            _isInvulnerable = true;
            _timer.Setup(_invulnerabilityWindow);
            _timer.StartCount();

            _value = Mathf.Max(0, _value - amount);

            ValueChanged?.Invoke(_value);
            Damaged?.Invoke();

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

            _value = Mathf.Min(_maxValue, _value + amount);

            ValueChanged?.Invoke(_value);
        }

        public void TurnOnInvulnerabilityWindow(float time)
        {
            if (time <= 0f)
            {
                throw new ArgumentOutOfRangeException("time");
            }

            _timer.Stop();
            _timer.Setup(time);

            _isInvulnerable = true;
            _timer.StartCount();
        }

        private void OnIFramesTimerFinished()
        {
            _isInvulnerable = false;
            InvulnerabilityEnded?.Invoke();
        }
    }
}