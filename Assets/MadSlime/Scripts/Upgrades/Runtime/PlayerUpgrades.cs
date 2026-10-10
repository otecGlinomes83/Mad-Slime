using System;
using System.Collections.Generic;
using Game;
using Saves;
using UnityEngine;
using VContainer;

namespace Upgrades
{
    public class PlayerUpgrades : MonoBehaviour
    {
        [SerializeField] private UpgradesConfig _config;

        private IUpgradesStorage _storage;

        public float GetSpeedMultiplier()
        {
            return GetMultiplier(UpgradeType.Speed);
        }

        public float GetMassMultiplier()
        {
            return GetMultiplier(UpgradeType.Appetite);
        }

        public float GetQuotaMassMultiplier()
        {
            return GetMultiplier(UpgradeType.Appetite) * GetMultiplier(UpgradeType.Taste);
        }

        public float GetForeignFillMultiplier()
        {
            return GetMultiplier(UpgradeType.Metabolism);
        }

        public bool HasSmell()
        {
            return IsPerkPurchased(PerkType.Smell);
        }

        public bool HasAdrenaline()
        {
            return IsPerkPurchased(PerkType.Adrenaline);
        }

        public bool HasAmbitions()
        {
            return IsPerkPurchased(PerkType.Ambitions);
        }

        [Inject]
        public void Construct(IUpgradesStorage storage)
        {
            _storage = storage;
        }

        private void Awake()
        {
            if (_config == null)
            {
                throw new InvalidOperationException(
                    $"{name}: UpgradesConfig is not assigned. Drag the UpgradesConfig asset into the _config field.");
            }

            if (_storage == null)
            {
                throw new InvalidOperationException(
                    $"{name}: IUpgradesStorage was not injected. Check that ProjectLifetimeScope registers Saver and PlayerUpgrades.");
            }

            for (int i = 0; i < _config.Upgrades.Count; i++)
            {
                UpgradeEntry entry = _config.Upgrades[i];

                if (entry.StepValues.Count != entry.MaxSteps)
                {
                    throw new InvalidOperationException(
                        $"PlayerUpgrades: upgrade '{entry.Type}' in '{_config.name}' has {entry.StepValues.Count} " +
                        $"step values, but MaxSteps is {entry.MaxSteps}. Fill one value per step.");
                }
            }

            for (int i = 0; i < _config.Perks.Count; i++)
            {
                GetPerkCost(_config.Perks[i].Type);
            }
        }

        public int GetLevel(UpgradeType type)
        {
            return _storage.GetUpgradeLevel(type);
        }

        public bool IsMaxed(UpgradeType type)
        {
            return GetLevel(type) >= GetUpgrade(type).MaxSteps;
        }

        public int GetNextCost(UpgradeType type)
        {
            if (IsMaxed(type) == true)
            {
                throw new InvalidOperationException(
                    $"PlayerUpgrades: upgrade '{type}' is maxed and has no next cost.");
            }

            UpgradeEntry entry = GetUpgrade(type);
            return entry.BaseCost + entry.CostStep * GetLevel(type);
        }

        public void PurchaseStepped(UpgradeType type)
        {
            if (IsMaxed(type) == true)
            {
                throw new InvalidOperationException(
                    $"PlayerUpgrades: upgrade '{type}' is already maxed.");
            }

            _storage.SetUpgradeLevel(type, GetLevel(type) + 1);
        }

        public bool IsPerkPurchased(PerkType type)
        {
            return _storage.IsPerkPurchased(type);
        }

        public int GetPerkCost(PerkType type)
        {
            return GetPerk(type).Cost;
        }

        public void PurchasePerk(PerkType type)
        {
            if (IsPerkPurchased(type) == true)
            {
                throw new InvalidOperationException(
                    $"PlayerUpgrades: perk '{type}' is already purchased.");
            }

            GetPerk(type);

            _storage.PurchasePerk(type);
        }

        public UpgradesConfig Config => _config;

        public IReadOnlyList<UpgradeEntry> UpgradeEntries => _config.Upgrades;

        public IReadOnlyList<PerkEntry> PerkEntries => _config.Perks;

        public int GetMaxSteps(UpgradeType type)
        {
            return GetUpgrade(type).MaxSteps;
        }

        public float GetNextStepValue(UpgradeType type)
        {
            UpgradeEntry entry = GetUpgrade(type);

            return SumValues(entry, GetLevel(type) + 1);
        }

        public float GetTotalValue(UpgradeType type)
        {
            UpgradeEntry entry = GetUpgrade(type);

            return SumValues(entry, GetLevel(type));
        }

        private float GetMultiplier(UpgradeType type)
        {
            UpgradeEntry entry = GetUpgrade(type);

            return 1f + SumValues(entry, GetLevel(type));
        }
        public UpgradeEntry GetUpgrade(UpgradeType type)
        {
            for (int i = 0; i < _config.Upgrades.Count; i++)
            {
                if (_config.Upgrades[i].Type == type)
                {
                    return _config.Upgrades[i];
                }
            }

            throw new InvalidOperationException(
                $"UpgradesConfig '{_config.name}': no entry for upgrade '{type}'. Add a row for it.");
        }

        public PerkEntry GetPerk(PerkType type)
        {
            for (int i = 0; i < _config.Perks.Count; i++)
            {
                if (_config.Perks[i].Type == type)
                {
                    return _config.Perks[i];
                }
            }

            throw new InvalidOperationException(
                $"UpgradesConfig '{_config.name}': no entry for perk '{type}'. Add a row for it.");
        }
        private float SumValues(UpgradeEntry entry, int level)
        {
            float total = 0f;

            for (int i = 0; i < level && i < entry.StepValues.Count; i++)
            {
                total += entry.StepValues[i];
            }

            return total;
        }
    }
}
