using System;
using System.Collections.Generic;
using System.Globalization;
using Game;
using UnityEngine;
using Upgrades;

namespace Shop
{
    public class UpgradesPagePresenter : MonoBehaviour
    {
        private const float MillionsPriceThreshold = 1000000f;
        private const float ThousandsPriceThreshold = 1000f;

        [SerializeField] private Transform _upgradesParent;
        [SerializeField] private UpgradeItemViewFactory _upgradeFactory;
        [SerializeField] private GameObject _perkSeparatorPrefab;

        private PlayerUpgrades _upgrades;
        private Wallet _wallet;
        private UpgradePurchaseModel _purchases;
        private List<UpgradeItemView> _upgradeViews = new List<UpgradeItemView>();
        private List<UpgradeItemView> _perkViews = new List<UpgradeItemView>();
        private GameObject _separator;

        public void Setup(PlayerUpgrades upgrades, Wallet wallet, Action<Action> transaction)
        {
            Clear();
            _upgrades = upgrades;
            _wallet = wallet;
            _purchases = new UpgradePurchaseModel(upgrades, wallet, transaction);

            if (isActiveAndEnabled == true)
            {
                Show();
            }
        }

        private void OnEnable()
        {
            if (_purchases != null)
            {
                Show();
            }
        }

        private void OnDisable()
        {
            Clear();
        }

        private void Show()
        {
            if (_upgradeViews.Count > 0 || _perkViews.Count > 0)
            {
                return;
            }

            foreach (UpgradeEntry entry in _upgrades.Config.Upgrades)
            {
                UpgradeItemView view = _upgradeFactory.Get(_upgradesParent);
                view.Initialize(entry.Icon, Localization.Get(GetUpgradeKey(entry.Type)), string.Empty, true);
                view.Click += OnClicked;
                _upgradeViews.Add(view);
                UpdateUpgrade(view, entry.Type);
            }

            if (_perkSeparatorPrefab != null)
            {
                _separator = Instantiate(_perkSeparatorPrefab, _upgradesParent);
            }

            foreach (PerkEntry entry in _upgrades.Config.Perks)
            {
                UpgradeItemView view = _upgradeFactory.Get(_upgradesParent);
                view.Initialize(entry.Icon, Localization.Get(GetPerkKey(entry.Type)),
                    Localization.Get(GetPerkDescKey(entry.Type)), false);
                view.Click += OnClicked;
                _perkViews.Add(view);
                UpdatePerk(view, entry.Type);
            }

            _wallet.BalanceChanged += OnBalanceChanged;
        }

        private void OnClicked(UpgradeItemView view)
        {
            int index = _upgradeViews.IndexOf(view);

            if (index >= 0)
            {
                UpgradeType type = _upgrades.Config.Upgrades[index].Type;

                if (_purchases.Purchase(type) == true)
                {
                    UpdateUpgrade(view, type);
                }

                return;
            }

            index = _perkViews.IndexOf(view);

            if (index >= 0)
            {
                PerkType type = _upgrades.Config.Perks[index].Type;

                if (_purchases.Purchase(type) == true)
                {
                    UpdatePerk(view, type);
                }
            }
        }

        private void UpdateUpgrade(UpgradeItemView view, UpgradeType type)
        {
            float value = _upgrades.GetTotalValue(type);

            if (_upgrades.IsMaxed(type) == false)
            {
                value = _upgrades.GetNextStepValue(type);
            }

            view.SetProgress(string.Format(Localization.Get("upgrade_step"),
                _upgrades.GetLevel(type), _upgrades.GetMaxSteps(type)),
                string.Format(Localization.Get(GetUpgradeDescKey(type)), FormatPercent(value)));
            SetUpgradeState(view, type);
        }

        private void SetUpgradeState(UpgradeItemView view, UpgradeType type)
        {
            if (_upgrades.IsMaxed(type) == true)
            {
                view.SetPurchaseState(Localization.Get("shop_max"), false);
                return;
            }

            view.SetPurchaseState(FormatPrice(_upgrades.GetNextCost(type)), _purchases.CanPurchase(type));
        }

        private void UpdatePerk(UpgradeItemView view, PerkType type)
        {
            if (_upgrades.IsPerkPurchased(type) == true)
            {
                view.SetPurchaseState(Localization.Get("shop_purchased"), false);
                return;
            }

            view.SetPurchaseState(FormatPrice(_upgrades.GetPerkCost(type)), _purchases.CanPurchase(type));
        }

        private void OnBalanceChanged(int previousBalance, int currentBalance)
        {
            for (int i = 0; i < _upgradeViews.Count; i++)
            {
                SetUpgradeState(_upgradeViews[i], _upgrades.Config.Upgrades[i].Type);
            }

            for (int i = 0; i < _perkViews.Count; i++)
            {
                UpdatePerk(_perkViews[i], _upgrades.Config.Perks[i].Type);
            }
        }

        private void Clear()
        {
            if (_wallet != null)
            {
                _wallet.BalanceChanged -= OnBalanceChanged;
            }

            foreach (UpgradeItemView view in _upgradeViews)
            {
                view.Click -= OnClicked;
                Destroy(view.gameObject);
            }

            foreach (UpgradeItemView view in _perkViews)
            {
                view.Click -= OnClicked;
                Destroy(view.gameObject);
            }

            _upgradeViews.Clear();
            _perkViews.Clear();

            if (_separator != null)
            {
                Destroy(_separator);
                _separator = null;
            }
        }

        private string FormatPercent(float value)
        {
            int percent = Mathf.RoundToInt(value * 100f);

            return percent.ToString(CultureInfo.InvariantCulture);
        }

        private string FormatPrice(int amount)
        {
            if (amount >= MillionsPriceThreshold)
            {
                return FormatFraction(amount / MillionsPriceThreshold) + "m";
            }

            if (amount >= ThousandsPriceThreshold)
            {
                return FormatFraction(amount / ThousandsPriceThreshold) + "k";
            }

            return amount.ToString(CultureInfo.InvariantCulture);
        }

        private string FormatFraction(float value)
        {
            return value.ToString("0.#", CultureInfo.InvariantCulture);
        }

        private string GetUpgradeKey(UpgradeType type)
        {
            switch (type)
            {
                case UpgradeType.Speed:
                    return "upgrade_speed";

                case UpgradeType.Appetite:
                    return "upgrade_appetite";

                case UpgradeType.Taste:
                    return "upgrade_taste";

                case UpgradeType.Metabolism:
                    return "upgrade_metabolism";

                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(type),
                        type,
                        "UpgradesPagePresenter received an unknown upgrade type.");
            }
        }

        private string GetUpgradeDescKey(UpgradeType type)
        {
            switch (type)
            {
                case UpgradeType.Speed:
                    return "upgrade_speed_desc";

                case UpgradeType.Appetite:
                    return "upgrade_appetite_desc";

                case UpgradeType.Taste:
                    return "upgrade_taste_desc";

                case UpgradeType.Metabolism:
                    return "upgrade_metabolism_desc";

                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(type),
                        type,
                        "UpgradesPagePresenter received an unknown upgrade type.");
            }
        }

        private string GetPerkKey(PerkType type)
        {
            switch (type)
            {
                case PerkType.Smell:
                    return "perk_smell";

                case PerkType.Adrenaline:
                    return "perk_adrenaline";

                case PerkType.Ambitions:
                    return "perk_ambitions";

                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(type),
                        type,
                        "UpgradesPagePresenter received an unknown perk type.");
            }
        }

        private string GetPerkDescKey(PerkType type)
        {
            switch (type)
            {
                case PerkType.Smell:
                    return "perk_smell_desc";

                case PerkType.Adrenaline:
                    return "perk_adrenaline_desc";

                case PerkType.Ambitions:
                    return "perk_ambitions_desc";

                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(type),
                        type,
                        "UpgradesPagePresenter received an unknown perk type.");
            }
        }
    }
}
