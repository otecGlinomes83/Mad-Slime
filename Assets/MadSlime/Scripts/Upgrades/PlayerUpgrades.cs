using System;
using System.Collections.Generic;
using Game;
using UnityEngine;
using VContainer;

namespace Upgrades
{
    public sealed class PlayerUpgrades : MonoBehaviour
    {
        [SerializeField] private UpgradesConfig _config;

        private PlayerProgress _progress;

        public float SpeedMultiplier => GetMultiplier(UpgradeType.Speed);
        public float MassMultiplier => GetMultiplier(UpgradeType.Appetite);
        public float QuotaMassMultiplier => GetMultiplier(UpgradeType.Appetite) * GetMultiplier(UpgradeType.Taste);
        public float ForeignFillMultiplier => GetMultiplier(UpgradeType.Metabolism);
        public bool HasSmell => IsPerkPurchased(PerkType.Smell);
        public bool HasAdrenaline => IsPerkPurchased(PerkType.Adrenaline);
        public bool HasAmbitions => IsPerkPurchased(PerkType.Ambitions);
        public int AmbitionTierOffset => HasAmbitions ? 1 : 0;
        public float AdrenalineSpeedMultiplier => _config.AdrenalineSpeedMultiplier;
        public float AdrenalineThresholdFraction => _config.AdrenalineThresholdFraction;
        public Color HighlightColor => _config.HighlightColor;

        [Inject]
        public void Construct(PlayerProgress progress)
        {
            _progress = progress;
        }

        private void Awake()
        {
            if (_config == null)
            {
                throw new InvalidOperationException(
                    $"{name}: UpgradesConfig is not assigned. Drag the UpgradesConfig asset into the _config field.");
            }

            if (_progress == null)
            {
                throw new InvalidOperationException(
                    $"{name}: PlayerProgress was not injected. Check that ProjectLifetimeScope registers PlayerProgress and PlayerUpgrades.");
            }

            for (int i = 0; i < _config.Upgrades.Count; i++)
            {
                UpgradeEntry entry = _config.Upgrades[i];
                _progress.GetUpgradeLevel(entry.Type);
            }

            for (int i = 0; i < _config.Perks.Count; i++)
            {
                GetPerkCost(_config.Perks[i].Type);
            }
        }

        public int GetLevel(UpgradeType type)
        {
            return _progress.GetUpgradeLevel(type);
        }

        public bool IsMaxed(UpgradeType type)
        {
            return GetLevel(type) >= _config.GetUpgrade(type).MaxSteps;
        }

        public int GetNextCost(UpgradeType type)
        {
            if (IsMaxed(type) == true)
            {
                throw new InvalidOperationException(
                    $"PlayerUpgrades: upgrade '{type}' is maxed and has no next cost.");
            }

            return _config.GetUpgrade(type).GetCost(GetLevel(type));
        }

        public void PurchaseStepped(UpgradeType type)
        {
            if (IsMaxed(type) == true)
            {
                throw new InvalidOperationException(
                    $"PlayerUpgrades: upgrade '{type}' is already maxed.");
            }

            _progress.SetUpgradeLevel(type, GetLevel(type) + 1);
            _progress.Save();
        }

        public bool IsPerkPurchased(PerkType type)
        {
            return _progress.PurchasedPerks.Contains(type);
        }

        public int GetPerkCost(PerkType type)
        {
            return _config.GetPerk(type).Cost;
        }

        public void PurchasePerk(PerkType type)
        {
            if (IsPerkPurchased(type) == true)
            {
                throw new InvalidOperationException(
                    $"PlayerUpgrades: perk '{type}' is already purchased.");
            }

            _config.GetPerk(type);

            _progress.PurchasedPerks.Add(type);
            _progress.Save();
        }

        public IReadOnlyList<UpgradeEntry> UpgradeEntries => _config.Upgrades;

        public IReadOnlyList<PerkEntry> PerkEntries => _config.Perks;

        public int GetMaxSteps(UpgradeType type)
        {
            return _config.GetUpgrade(type).MaxSteps;
        }

        private float GetMultiplier(UpgradeType type)
        {
            UpgradeEntry entry = _config.GetUpgrade(type);

            return 1f + entry.ValuePerStep * GetLevel(type);
        }
    }
}
