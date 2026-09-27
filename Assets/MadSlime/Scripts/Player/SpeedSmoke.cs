using Movement;
using Scriptables;
using System;
using UnityEngine;
using VContainer;

namespace Player
{
    public sealed class SpeedSmoke : MonoBehaviour
    {
        [SerializeField] private ParticleSystem _prefab;

        [SerializeField, Min(0f)] private float _smokeRate = 35f;

        private PlayerConfig _config;
        private Mover _mover;
        private ParticleSystem _instance;
        private ParticleSystem.EmissionModule _emission;
        private bool _isSmoking;

        [Inject]
        public void Construct(PlayerConfig config, Mover mover)
        {
            _config = config;
            _mover = mover;
        }

        private void Awake()
        {
            if (_prefab == null)
            {
                throw new InvalidOperationException(
                    $"{name}: Smoke prefab is not assigned. Drag a ParticleSystem prefab into the _prefab field.");
            }

            if (_config == null)
            {
                throw new InvalidOperationException(
                    $"{name}: PlayerConfig was not injected. Check that GameLifetimeScope registers SpeedSmoke.");
            }

            if (_mover == null)
            {
                throw new InvalidOperationException(
                    $"{name}: Mover was not injected. Check that GameLifetimeScope registers SpeedSmoke.");
            }

            _instance = Instantiate(_prefab, transform);
            _emission = _instance.emission;
            _emission.rateOverTime = 0f;
        }

        private void Update()
        {
            if (_config.SmokeSpeedThreshold <= 0f)
            {
                SetSmoking(false);
                return;
            }

            SetSmoking(_mover.Velocity.magnitude >= _config.SmokeSpeedThreshold);
        }

        private void SetSmoking(bool isSmoking)
        {
            if (_isSmoking == isSmoking)
            {
                return;
            }

            _isSmoking = isSmoking;
            _emission.rateOverTime = isSmoking == true ? _smokeRate : 0f;
        }
    }
}
