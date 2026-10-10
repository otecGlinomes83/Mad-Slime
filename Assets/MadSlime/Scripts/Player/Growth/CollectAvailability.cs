using Skills;
using System;
using UnityEngine;
using Upgrades;
using VContainer;

namespace Player
{
    public class CollectAvailability : MonoBehaviour
    {
        private const int AmbitionTierOffset = 1;

        private PlayerTier _playerTier;
        private PlayerUpgrades _upgrades;

        [Inject]
        public void Construct(PlayerTier playerTier, PlayerUpgrades upgrades)
        {
            _playerTier = playerTier;
            _upgrades = upgrades;
        }

        private void Awake()
        {
            if (_playerTier == null)
            {
                throw new InvalidOperationException(
                    $"{name}: PlayerTier was not injected. Check that GameLifetimeScope registers PlayerTier and CollectAvailability.");
            }

            if (_upgrades == null)
            {
                throw new InvalidOperationException(
                    $"{name}: PlayerUpgrades was not injected. Check that ProjectLifetimeScope registers PlayerUpgrades and CollectAvailability.");
            }
        }

        public bool CanCollect(SizeTier itemTier)
        {
            int allowedTier = (int)_playerTier.CurrentTier;

            if (_upgrades.HasAmbitions == true)
            {
                allowedTier += AmbitionTierOffset;
            }

            return (int)itemTier <= allowedTier;
        }
    }
}
