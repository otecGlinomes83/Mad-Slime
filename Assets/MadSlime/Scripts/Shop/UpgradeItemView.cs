using System;
using Game;
using TMPro;
using UI;
using Upgrades;
using UnityEngine;
using UnityEngine.UI;

namespace Skins
{
    [RequireComponent(typeof(Image))]
    [RequireComponent(typeof(Button))]
    public sealed class UpgradeItemView : MonoBehaviour
    {
        [SerializeField] private TMP_Text _titleText;
        [SerializeField] private TMP_Text _stateText;
        [SerializeField] private IntValueView _priceView;

        private Button _button;
        private PlayerUpgrades _upgrades;
        private UpgradeType _upgradeType;
        private PerkType _perkType;
        private bool _isPerk;

        public event Action<UpgradeItemView> Click;

        public bool IsPerk => _isPerk;

        public UpgradeType UpgradeType => _upgradeType;

        public PerkType PerkType => _perkType;

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

        private void Setup()
        {
            _button = GetComponent<Button>();
            _button.onClick.AddListener(OnClick);

            Refresh();
        }

        public void Refresh()
        {
            if (_isPerk == true)
            {
                bool purchased = _upgrades.IsPerkPurchased(_perkType);

                _titleText.text = Localization.Get(GetPerkKey(_perkType));

                if (purchased == true)
                {
                    _stateText.text = Localization.Get("shop_purchased");
                    _stateText.gameObject.SetActive(true);
                    _priceView.Hide();
                    _button.interactable = false;
                }
                else
                {
                    _stateText.gameObject.SetActive(false);
                    _priceView.Show(_upgrades.GetPerkCost(_perkType));
                    _button.interactable = true;
                }

                return;
            }

            int level = _upgrades.GetLevel(_upgradeType);
            bool maxed = _upgrades.IsMaxed(_upgradeType);

            _titleText.text = Localization.Get(GetUpgradeKey(_upgradeType));

            if (maxed == true)
            {
                _stateText.text = Localization.Get("shop_max");
                _stateText.gameObject.SetActive(true);
                _priceView.Hide();
                _button.interactable = false;
            }
            else
            {
                _stateText.text = $"{level}/{_upgrades.GetMaxSteps(_upgradeType)}";
                _stateText.gameObject.SetActive(true);
                _priceView.Show(_upgrades.GetNextCost(_upgradeType));
                _button.interactable = true;
            }
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
    }
}
