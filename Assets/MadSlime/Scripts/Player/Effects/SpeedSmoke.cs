using Movement;
using Scriptables;
using System;
using UnityEngine;
using VContainer;

namespace Player
{
    public class SpeedSmoke : MonoBehaviour
    {
        [Tooltip("Заранее расставленный в сцене зацикленный партикл дыма: код только включает и выключает проигрывание, настройки не трогает.")]
        [SerializeField] private ParticleSystem _smoke;

        private PlayerConfig _config;
        private Movement.Movement _movement;
        private bool _isSmoking;

        [Inject]
        public void Construct(PlayerConfig config, Movement.Movement movement)
        {
            _config = config;
            _movement = movement;
        }

        private void Awake()
        {
            if (_smoke == null)
            {
                throw new InvalidOperationException(
                    $"{name}: Smoke is not assigned. Place a looping smoke ParticleSystem in the scene and drag it into the _smoke field.");
            }

            if (_config == null)
            {
                throw new InvalidOperationException(
                    $"{name}: PlayerConfig was not injected. Check that GameLifetimeScope registers PlayerConfig and SpeedSmoke.");
            }

            if (_movement == null)
            {
                throw new InvalidOperationException(
                    $"{name}: Movement was not injected. Check that GameLifetimeScope registers Movement and SpeedSmoke.");
            }

            _smoke.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }

        private void OnEnable()
        {
            _movement.SpeedChanged += OnSpeedChanged;

            Refresh();
        }

        private void OnDisable()
        {
            _movement.SpeedChanged -= OnSpeedChanged;

            SetSmoking(false);
        }

        private void OnSpeedChanged()
        {
            Refresh();
        }

        private void Refresh()
        {
            float threshold = _config.SmokeSpeedThreshold;

            SetSmoking(threshold > 0f && _movement.CurrentSpeed > threshold);
        }

        private void SetSmoking(bool isSmoking)
        {
            if (_isSmoking == isSmoking)
            {
                return;
            }

            _isSmoking = isSmoking;

            if (isSmoking == true)
            {
                _smoke.Play(true);
            }
            else
            {
                _smoke.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            }
        }
    }
}
