using Scriptables;
using System;
using UnityEngine;
using VContainer;

namespace Player
{
    public sealed class SpeedSmoke : MonoBehaviour
    {
        [Tooltip("Заранее расставленный в сцене зацикленный партикл дыма: код только включает и выключает проигрывание, настройки не трогает.")]
        [SerializeField] private ParticleSystem _smoke;

        private PlayerConfig _config;
        private PlayerSpeed _playerSpeed;
        private bool _isSmoking;

        [Inject]
        public void Construct(PlayerConfig config, PlayerSpeed playerSpeed)
        {
            _config = config;
            _playerSpeed = playerSpeed;
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

            if (_playerSpeed == null)
            {
                throw new InvalidOperationException(
                    $"{name}: PlayerSpeed was not injected. Check that GameLifetimeScope registers PlayerSpeed and SpeedSmoke.");
            }

            _smoke.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }

        private void OnEnable()
        {
            _playerSpeed.SpeedChanged += OnSpeedChanged;

            Refresh();
        }

        private void OnDisable()
        {
            _playerSpeed.SpeedChanged -= OnSpeedChanged;

            SetSmoking(false);
        }

        private void OnSpeedChanged()
        {
            Refresh();
        }

        private void Refresh()
        {
            float threshold = _config.SmokeSpeedThreshold;

            SetSmoking(threshold > 0f && _playerSpeed.CurrentSpeed > threshold);
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
