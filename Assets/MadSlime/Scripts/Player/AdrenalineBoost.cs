using Game;
using System;
using UnityEngine;
using Upgrades;
using VContainer;

namespace Player
{
    public sealed class AdrenalineBoost : MonoBehaviour
    {
        private Timer _timer;
        private PlayerUpgrades _upgrades;
        private PlayerSpeed _playerSpeed;
        private bool _isBoosted;

        public event Action BoostStarted;
        public event Action BoostEnded;

        [Inject]
        public void Construct(Timer timer, PlayerUpgrades upgrades, PlayerSpeed playerSpeed)
        {
            _timer = timer;
            _upgrades = upgrades;
            _playerSpeed = playerSpeed;
        }

        private void Awake()
        {
            if (_timer == null)
            {
                throw new InvalidOperationException(
                    $"{name}: Timer was not injected. Check that GameLifetimeScope registers Timer and AdrenalineBoost.");
            }

            if (_upgrades == null)
            {
                throw new InvalidOperationException(
                    $"{name}: PlayerUpgrades was not injected. Check that ProjectLifetimeScope registers PlayerUpgrades.");
            }

            if (_playerSpeed == null)
            {
                throw new InvalidOperationException(
                    $"{name}: PlayerSpeed was not injected. Check that GameLifetimeScope registers PlayerSpeed and AdrenalineBoost.");
            }
        }

        private void OnEnable()
        {
            _timer.FinalCountdownStarted += OnFinalCountdownStarted;
            _timer.Finished += OnTimerFinished;
            _timer.Stopped += OnTimerStopped;
        }

        private void OnDisable()
        {
            _timer.FinalCountdownStarted -= OnFinalCountdownStarted;
            _timer.Finished -= OnTimerFinished;
            _timer.Stopped -= OnTimerStopped;

            ResetBoost();
        }

        private void OnFinalCountdownStarted()
        {
            if (_upgrades.HasAdrenaline == false)
            {
                return;
            }

            _isBoosted = true;
            _playerSpeed.SetBoostMultiplier(_upgrades.AdrenalineSpeedMultiplier);

            BoostStarted?.Invoke();
        }

        private void OnTimerFinished()
        {
            ResetBoost();
        }

        private void OnTimerStopped()
        {
            ResetBoost();
        }

        private void ResetBoost()
        {
            if (_isBoosted == false)
            {
                return;
            }

            _isBoosted = false;
            _playerSpeed.SetBoostMultiplier(1f);

            BoostEnded?.Invoke();
        }
    }
}
