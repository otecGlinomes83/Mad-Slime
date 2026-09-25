using Game;
using Movement;
using System;
using UnityEngine;
using Upgrades;
using VContainer;

namespace Player
{
    public sealed class AdrenalineBoost : MonoBehaviour
    {
        private Timer _timer;
        private Mover _mover;
        private PlayerUpgrades _upgrades;
        private bool _isBoosted;

        [Inject]
        public void Construct(Timer timer, Mover mover, PlayerUpgrades upgrades)
        {
            _timer = timer;
            _mover = mover;
            _upgrades = upgrades;
        }

        private void Awake()
        {
            if (_timer == null)
            {
                throw new InvalidOperationException(
                    $"{name}: Timer was not injected. Check that GameLifetimeScope registers Timer and AdrenalineBoost.");
            }

            if (_mover == null)
            {
                throw new InvalidOperationException(
                    $"{name}: Mover was not injected. Check that GameLifetimeScope registers Mover and AdrenalineBoost.");
            }

            if (_upgrades == null)
            {
                throw new InvalidOperationException(
                    $"{name}: PlayerUpgrades was not injected. Check that ProjectLifetimeScope registers PlayerUpgrades.");
            }
        }

        private void OnEnable()
        {
            _timer.Ticked += OnTimerTicked;
            _timer.Finished += OnTimerFinished;
        }

        private void OnDisable()
        {
            _timer.Ticked -= OnTimerTicked;
            _timer.Finished -= OnTimerFinished;

            ResetBoost();
        }

        private void OnTimerTicked(float remaining)
        {
            if (_upgrades.HasAdrenaline == false)
            {
                ResetBoost();
                return;
            }

            float threshold = _timer.Duration * _upgrades.AdrenalineThresholdFraction;
            bool shouldBeBoosted = remaining > 0f && remaining <= threshold;

            if (shouldBeBoosted == _isBoosted)
            {
                return;
            }

            _isBoosted = shouldBeBoosted;

            if (shouldBeBoosted == true)
            {
                _mover.SetSpeedMultiplier(_upgrades.AdrenalineSpeedMultiplier);
            }
            else
            {
                _mover.SetSpeedMultiplier(1f);
            }
        }

        private void OnTimerFinished()
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
            _mover.SetSpeedMultiplier(1f);
        }
    }
}
