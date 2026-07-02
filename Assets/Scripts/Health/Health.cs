using System;
using Skills;
using UnityEngine;

namespace Assets.Scripts.HealthSystem
{
    public sealed class Health : MonoBehaviour
    {
        [SerializeField] private int _maxValue;
        [SerializeField] private int _value;
        [SerializeField] private Invulnerability _invulnerability;
        [SerializeField] private DodgeSkill _dodgeSkill;
        [SerializeField] private SkillTracker _skillManager;

        public event Action Died;
        public event Action Damaged;
        public event Action DamageDodged;
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
                throw new ArgumentOutOfRangeException(nameof(_value), "HealthSystem value cannot be negative");
            }

            if (_invulnerability == null)
            {
                throw new InvalidOperationException(
                    $"{name}: Invulnerability is not assigned. Drag an Invulnerability component into the _invulnerability field.");
            }

            _value = _maxValue;
        }

        private void Start()
        {
            ValueChanged?.Invoke(_value);
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

            if (CanDodge() == true && _dodgeSkill.TryDodge() == true)
            {
                DamageDodged?.Invoke();
                _invulnerability.EnterWindow();
                return;
            }

            if (_invulnerability.IsInvulnerable == true)
            {
                return;
            }

            _invulnerability.EnterWindow();

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
            _invulnerability.EnterWindow(time);
        }

        private bool CanDodge()
        {
            if (_dodgeSkill == null)
            {
                return false;
            }

            if (_skillManager == null)
            {
                return false;
            }

            return _skillManager.IsUnlocked(SkillId.Dodge);
        }
    }
}