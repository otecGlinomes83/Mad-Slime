using System;
using System.Globalization;
using Game;
using TMPro;
using Upgrades;
using UnityEngine;
using UnityEngine.UI;

namespace Skins
{
    [RequireComponent(typeof(Image))]
    [RequireComponent(typeof(Button))]
    public sealed class UpgradeItemView : MonoBehaviour
    {
        [SerializeField] private Image _icon;
        [SerializeField] private TMP_Text _stepText;
        [SerializeField] private TMP_Text _titleText;
        [SerializeField] private TMP_Text _effectText;
        [SerializeField] private TMP_Text _priceText;

        private Button _button;
        private PlayerUpgrades _upgrades;
        private UpgradeType _upgradeType;
        private PerkType _perkType;
        private bool _isPerk;

        public event Action<UpgradeItemView> Click;

        public bool IsPerk => _isPerk;

        public UpgradeType UpgradeType => _upgradeType;

        public PerkType PerkType => _perkType;

        private void Awake()
        {
            if (_icon == null || _stepText == null || _titleText == null || _effectText == null || _priceText == null)
            {
                throw new InvalidOperationException(
                    $"{name}: a card part is not assigned. Drag the Icon, Step, Title, Effect and Price objects into the fields.");
            }
        }

        public void Initialize(PlayerUpgrades upgrades, UpgradeType type)
        {
            _upgrades = upgrades;
            _upgradeType = type;
            _isPerk = false;

            Setup();
        }

        public void Initialize(PlayerUpgrades upgrades, PerkType type)
        {
            _upgrades = upgrades;
            _perkType = type;
            _isPerk = true;

            Setup();
        }

        public void Refresh()
        {
            if (_isPerk == true)
            {
                RefreshPerk();
                return;
            }

            RefreshUpgrade();
        }

        private void Setup()
        {
            _button = GetComponent<Button>();
            _button.onClick.AddListener(OnClick);

            Refresh();
        }

        private void RefreshUpgrade()
        {
            int level = _upgrades.GetLevel(_upgradeType);
            int maxSteps = _upgrades.GetMaxSteps(_upgradeType);

            ApplyIcon(_upgrades.GetUpgradeIcon(_upgradeType));
            _titleText.text = Localization.Get(GetUpgradeKey(_upgradeType));
            _stepText.text = string.Format(Localization.Get("upgrade_step"), level, maxSteps);
            _stepText.gameObject.SetActive(true);

            if (_upgrades.IsMaxed(_upgradeType) == true)
            {
                _effectText.text = string.Format(
                    Localization.Get(GetUpgradeDescKey(_upgradeType)),
                    FormatPercent(_upgrades.GetTotalValue(_upgradeType)));
                _priceText.text = Localization.Get("shop_max");
                _button.interactable = false;

                return;
            }

            _effectText.text = string.Format(
                Localization.Get(GetUpgradeDescKey(_upgradeType)),
                FormatPercent(_upgrades.GetNextStepValue(_upgradeType)));
            _priceText.text = FormatPrice(_upgrades.GetNextCost(_upgradeType));
            _button.interactable = true;
        }

        private void RefreshPerk()
        {
            ApplyIcon(_upgrades.GetPerkIcon(_perkType));
            _titleText.text = Localization.Get(GetPerkKey(_perkType));
            _effectText.text = Localization.Get(GetPerkDescKey(_perkType));
            _stepText.gameObject.SetActive(false);

            if (_upgrades.IsPerkPurchased(_perkType) == true)
            {
                _priceText.text = Localization.Get("shop_purchased");
                _button.interactable = false;

                return;
            }

            _priceText.text = FormatPrice(_upgrades.GetPerkCost(_perkType));
            _button.interactable = true;
        }

        private void ApplyIcon(Sprite icon)
        {
            _icon.sprite = icon;
            _icon.enabled = icon != null;
        }

        private void OnClick()
        {
            Click?.Invoke(this);
        }

        private void OnDisable()
        {
            if (_button != null)
            {
                _button.onClick.RemoveListener(OnClick);
            }
        }

        private static string FormatPercent(float value)
        {
            int percent = Mathf.RoundToInt(value * 100f);

            return percent.ToString(CultureInfo.InvariantCulture);
        }

        private static string FormatPrice(int amount)
        {
            if (amount >= 1000000)
            {
                return FormatFraction(amount / 1000000f) + "m";
            }

            if (amount >= 1000)
            {
                return FormatFraction(amount / 1000f) + "k";
            }

            return amount.ToString(CultureInfo.InvariantCulture);
        }

        private static string FormatFraction(float value)
        {
            return value.ToString("0.#", CultureInfo.InvariantCulture);
        }

        private static string GetUpgradeKey(UpgradeType type)
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
                        "UpgradeItemView received an unknown upgrade type.");
            }
        }

        private static string GetUpgradeDescKey(UpgradeType type)
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
                        "UpgradeItemView received an unknown upgrade type.");
            }
        }

        private static string GetPerkKey(PerkType type)
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
                        "UpgradeItemView received an unknown perk type.");
            }
        }

        private static string GetPerkDescKey(PerkType type)
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
                        "UpgradeItemView received an unknown perk type.");
            }
        }
    }
}
