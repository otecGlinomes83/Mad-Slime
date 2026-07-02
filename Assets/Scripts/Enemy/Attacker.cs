using Interfaces;
using System;
using UnityEngine;

namespace NPC.Enemy
{
    public sealed class Attacker : MonoBehaviour
    {
        [SerializeField] private int _damage = 1;
        [SerializeField] private float _cooldown = 0.4f;
        [SerializeField] private Game.Timer _timer;

        private bool _isCooldown;

        public event Action AttackPerformed;

        private void Awake()
        {
            if (_timer == null)
            {
                throw new InvalidOperationException(
                    $"{name}: Timer is not assigned. Drag a Timer component into the _timer field in the inspector.");
            }

            _isCooldown = false;
        }

        private void OnEnable()
        {
            _timer.Finished += OnCooldownFinished;
        }

        private void OnDisable()
        {
            _timer.Finished -= OnCooldownFinished;
        }

        public void TryAttack(ITarget target)
        {
            if (_isCooldown)
            {
                return;
            }

            target.Health.TryApplyDamage(_damage);
            AttackPerformed?.Invoke();

            _isCooldown = true;
            _timer.Setup(_cooldown);
            _timer.StartCount();
        }

        private void OnCooldownFinished()
        {
            _isCooldown = false;
        }
    }
}