using System;
using Detectors;
using Game;
using Interfaces;
using UnityEngine;

namespace NPC.Enemy
{
    public sealed class Attacker : MonoBehaviour
    {
        [SerializeField] private TargetSensor _sensor;
        [SerializeField] private int _damage = 1;
        [SerializeField] private float _cooldown = 0.4f;
        [SerializeField] private Game.Timer _timer;

        private bool _isCooldown;

        private void Awake()
        {
            if (_sensor == null)
            {
                throw new InvalidOperationException(
                    $"{name}: TargetSensor is not assigned. Drag a TargetSensor component into the _sensor field in the inspector.");
            }

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

        private void Update()
        {
            if (_isCooldown)
            {
                return;
            }

            if (_sensor.IsTargetInRange == false)
            {
                return;
            }

            ITarget target = _sensor.DetectedTarget;

            if (target == null)
            {
                return;
            }

            target.Health.TryApplyDamage(_damage);
            Debug.Log("target has been damaged");

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