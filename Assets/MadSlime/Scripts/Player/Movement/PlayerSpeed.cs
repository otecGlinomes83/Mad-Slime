using Movement;
using Skills;
using System;
using Upgrades;
using UnityEngine;
using VContainer;

namespace Player
{
    public sealed class PlayerSpeed : MonoBehaviour
    {
        private PlayerTier _playerTier;
        private TierResolver _tierResolver;
        private PlayerUpgrades _upgrades;
        private Mover _mover;
        private float _boostMultiplier = 1f;

        public event Action SpeedChanged;

        public float CurrentSpeed { get; private set; }

        [Inject]
        public void Construct(PlayerTier playerTier, TierResolver tierResolver, PlayerUpgrades upgrades, Mover mover)
        {
            _playerTier = playerTier;
            _tierResolver = tierResolver;
            _upgrades = upgrades;
            _mover = mover;
        }

        private void Awake()
        {
            if (_playerTier == null)
            {
                throw new InvalidOperationException(
                    $"{name}: PlayerTier was not injected. Check that GameLifetimeScope registers PlayerTier and PlayerSpeed.");
            }

            if (_tierResolver == null)
            {
                throw new InvalidOperationException(
                    $"{name}: TierResolver was not injected. Check that GameLifetimeScope registers TierResolver and PlayerSpeed.");
            }

            if (_upgrades == null)
            {
                throw new InvalidOperationException(
                    $"{name}: PlayerUpgrades was not injected. Check that ProjectLifetimeScope registers PlayerUpgrades.");
            }

            if (_mover == null)
            {
                throw new InvalidOperationException(
                    $"{name}: Mover was not injected. Check that GameLifetimeScope registers Mover and PlayerSpeed.");
            }
        }

        private void OnEnable()
        {
            _playerTier.TierChanged += OnTierChanged;

            ApplySpeed();
        }

        private void OnDisable()
        {
            _playerTier.TierChanged -= OnTierChanged;
        }

        public void SetBoostMultiplier(float multiplier)
        {
            if (multiplier <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(multiplier),
                    "PlayerSpeed.SetBoostMultiplier requires a positive multiplier.");
            }

            _boostMultiplier = multiplier;

            ApplySpeed();
        }

        private void OnTierChanged(ItemTier previousTier, ItemTier currentTier)
        {
            ApplySpeed();
        }

        private void ApplySpeed()
        {
            float tierSpeed = _tierResolver.GetSpeedFor(_playerTier.CurrentTier);
            CurrentSpeed = tierSpeed * _upgrades.SpeedMultiplier * _boostMultiplier;

            _mover.SetSpeed(CurrentSpeed);

            SpeedChanged?.Invoke();
        }
    }
}
