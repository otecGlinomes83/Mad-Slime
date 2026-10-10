using Movement;
using Scriptables;
using System;
using UnityEngine;
using Upgrades;
using VContainer;

namespace Player
{
    public class AdrenalineBoost : MonoBehaviour
    {
        [SerializeField] private UpgradesConfig _config;

        private PlayerUpgrades _upgrades;
        private Movement.Movement _movement;
        private bool _isBoosted;

        public event Action BoostStarted;
        public event Action BoostEnded;

        [Inject]
        public void Construct(PlayerUpgrades upgrades, Movement.Movement movement)
        {
            _upgrades = upgrades;
            _movement = movement;
        }

        private void Awake()
        {
            if (_config == null)
            {
                throw new InvalidOperationException(
                    $"{name}: UpgradesConfig is not assigned. Drag the UpgradesConfig asset into the _config field.");
            }

            if (_upgrades == null)
            {
                throw new InvalidOperationException(
                    $"{name}: PlayerUpgrades was not injected. Check that ProjectLifetimeScope registers PlayerUpgrades.");
            }

            if (_movement == null)
            {
                throw new InvalidOperationException(
                    $"{name}: Movement was not injected. Check that GameLifetimeScope registers Movement and AdrenalineBoost.");
            }

            _config.GetPerk(PerkType.Adrenaline);
        }

        public void Enable()
        {
            if (_isBoosted == true)
            {
                return;
            }

            if (_upgrades.HasAdrenaline == false)
            {
                return;
            }

            _isBoosted = true;
            _movement.SetBoostMultiplier(_config.GetPerk(PerkType.Adrenaline).Value);

            BoostStarted?.Invoke();
        }

        public void Disable()
        {
            if (_isBoosted == false)
            {
                return;
            }

            _isBoosted = false;
            _movement.SetBoostMultiplier(1f);

            BoostEnded?.Invoke();
        }
    }
}
