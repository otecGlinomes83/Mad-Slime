using System;
using System.Collections.Generic;
using Game;
using TMPro;
using Upgrades;
using UnityEngine;
using UnityEngine.UI;
using VContainer;
using YG;

namespace Skins
{
    public sealed class ShopPanel : MonoBehaviour
    {
        [SerializeField] private Transform _itemsParent;
        [SerializeField] private Transform _upgradesParent;
        [SerializeField] private ShopItemViewFactory _factory;
        [SerializeField] private UpgradeItemViewFactory _upgradeFactory;
        [SerializeField] private TMP_Text _moneyText;
        [SerializeField] private Button _skinsTabButton;
        [SerializeField] private Button _upgradesTabButton;

        private readonly List<ShopItemView> _shopItems = new List<ShopItemView>();
        private readonly List<UpgradeItemView> _upgradeItems = new List<UpgradeItemView>();

        private Wallet _wallet;
        private PlayerUpgrades _upgrades;
        private readonly List<SkinItem> _skinItems = new List<SkinItem>();
        private readonly List<SkinItem> _exclusiveSkins = new List<SkinItem>();

        private ShopItemView _selectedView;

        public event Action<ShopItemView> ViewSelected;

        public ShopItemView SelectedView => _selectedView;

        [Inject]
        public void Construct(Wallet wallet, PlayerUpgrades upgrades)
        {
            _wallet = wallet;
            _upgrades = upgrades;
        }

        public void Initialize(IEnumerable<SkinItem> skinItems, IEnumerable<SkinItem> exclusiveSkins)
        {
            if (_wallet == null)
            {
                throw new InvalidOperationException(
                    $"{name}: Wallet was not injected. Check that ShopLifetimeScope registers Wallet and ShopPanel.");
            }

            if (_upgrades == null)
            {
                throw new InvalidOperationException(
                    $"{name}: PlayerUpgrades was not injected. Check that ProjectLifetimeScope registers PlayerUpgrades.");
            }

            if (_itemsParent == null)
            {
                throw new InvalidOperationException(
                    $"{name}: ItemsParent is not assigned. Drag a Transform into the _itemsParent field.");
            }

            if (_upgradesParent == null)
            {
                throw new InvalidOperationException(
                    $"{name}: UpgradesParent is not assigned. Drag a Transform into the _upgradesParent field.");
            }

            if (_factory == null)
            {
                throw new InvalidOperationException(
                    $"{name}: Factory is not assigned. Drag a ShopItemViewFactory component into the _factory field.");
            }

            if (_upgradeFactory == null)
            {
                throw new InvalidOperationException(
                    $"{name}: UpgradeFactory is not assigned. Drag an UpgradeItemViewFactory component into the _upgradeFactory field.");
            }

            if (_moneyText == null)
            {
                throw new InvalidOperationException(
                    $"{name}: MoneyText is not assigned. Drag a TMP_Text into the _moneyText field.");
            }

            if (_skinsTabButton == null || _upgradesTabButton == null)
            {
                throw new InvalidOperationException(
                    $"{name}: a tab button is not assigned. Drag the Skins and Upgrades tab Buttons into the fields.");
            }

            FillSkinList(skinItems);
            FillSkinList(exclusiveSkins, isExclusive: true);

            _wallet.BalanceChanged += OnBalanceChanged;
            _skinsTabButton.onClick.AddListener(ShowSkinsCategory);
            _upgradesTabButton.onClick.AddListener(ShowUpgradesCategory);

            OnBalanceChanged(_wallet.Balance, _wallet.Balance);
        }

        public void ShowSkinsCategory()
        {
            _itemsParent.gameObject.SetActive(true);
            _upgradesParent.gameObject.SetActive(false);

            PopulateSkins();
        }

        public void ShowUpgradesCategory()
        {
            _itemsParent.gameObject.SetActive(false);
            _upgradesParent.gameObject.SetActive(true);

            PopulateUpgrades();
        }

        private void FillSkinList(IEnumerable<SkinItem> source, bool isExclusive = false)
        {
            if (source == null)
            {
                return;
            }

            foreach (SkinItem item in source)
            {
                if (item == null)
                {
                    continue;
                }

                if (_skinItems.Contains(item) == false)
                {
                    _skinItems.Add(item);

                    if (isExclusive == true)
                    {
                        _exclusiveSkins.Add(item);
                    }
                }
            }
        }

        private void PopulateSkins()
        {
            Clear();

            foreach (SkinItem item in _skinItems)
            {
                ShopItemView view = _factory.Get(item, _itemsParent);
                view.Click += OnSkinItemClick;

                if (IsOpen(item) == true)
                {
                    view.SetExclusive(false);
                    view.Unlock();

                    if (IsSelected(item) == true)
                    {
                        ApplySelection(view);
                    }
                    else
                    {
                        view.UnSelect();
                    }
                }
                else
                {
                    view.SetExclusive(_exclusiveSkins.Contains(item));
                    view.Lock();
                    view.UnSelect();
                }

                _shopItems.Add(view);
            }
        }

        private void PopulateUpgrades()
        {
            Clear();

            foreach (UpgradeEntry entry in _upgrades.UpgradeEntries)
            {
                UpgradeItemView view = _upgradeFactory.Get(_upgrades, entry.Type, _upgradesParent);
                view.Click += OnUpgradeItemClick;

                _upgradeItems.Add(view);
            }

            foreach (PerkEntry entry in _upgrades.PerkEntries)
            {
                UpgradeItemView view = _upgradeFactory.Get(_upgrades, entry.Type, _upgradesParent);
                view.Click += OnUpgradeItemClick;

                _upgradeItems.Add(view);
            }
        }

        private void OnSkinItemClick(ShopItemView view)
        {
            if (view.IsLock == true)
            {
                return;
            }

            ViewSelected?.Invoke(view);
            ApplySelection(view);
            SelectPersist(view.SkinItem);
        }

        private void OnUpgradeItemClick(UpgradeItemView view)
        {
            if (view.IsPerk == true)
            {
                PerkType perkType = view.PerkType;

                if (_upgrades.IsPerkPurchased(perkType) == true)
                {
                    return;
                }

                int cost = _upgrades.GetPerkCost(perkType);

                if (_wallet.Balance < cost)
                {
                    return;
                }

                _wallet.Spend(cost);
                _upgrades.PurchasePerk(perkType);
            }
            else
            {
                UpgradeType upgradeType = view.UpgradeType;

                if (_upgrades.IsMaxed(upgradeType) == true)
                {
                    return;
                }

                int cost = _upgrades.GetNextCost(upgradeType);

                if (_wallet.Balance < cost)
                {
                    return;
                }

                _wallet.Spend(cost);
                _upgrades.PurchaseStepped(upgradeType);
            }

            foreach (UpgradeItemView upgradeItem in _upgradeItems)
            {
                upgradeItem.Refresh();
            }
        }

        private void ApplySelection(ShopItemView view)
        {
            if (_selectedView != null && _selectedView != view)
            {
                _selectedView.UnHighlight();
                _selectedView.UnSelect();
            }

            view.Highlight();
            view.Select();
            _selectedView = view;
        }

        private static bool IsOpen(SkinItem item)
        {
            return YG2.saves._openSkins.Contains(item.SkinType);
        }

        private static bool IsSelected(SkinItem item)
        {
            return YG2.saves.SelectedSkinType == item.SkinType;
        }

        private void SelectPersist(SkinItem item)
        {
            YG2.saves.SelectedSkinType = item.SkinType;

            if (YG2.isSDKEnabled == true)
            {
                YG2.SaveProgress();
            }
        }

        private void OnBalanceChanged(int previousBalance, int currentBalance)
        {
            _moneyText.text = currentBalance.ToString();
        }

        private void OnDisable()
        {
            if (_skinsTabButton != null)
            {
                _skinsTabButton.onClick.RemoveListener(ShowSkinsCategory);
            }

            if (_upgradesTabButton != null)
            {
                _upgradesTabButton.onClick.RemoveListener(ShowUpgradesCategory);
            }

            Clear();
        }

        private void OnDestroy()
        {
            if (_wallet != null)
            {
                _wallet.BalanceChanged -= OnBalanceChanged;
            }
        }

        private void Clear()
        {
            foreach (ShopItemView view in _shopItems)
            {
                view.Click -= OnSkinItemClick;
                Destroy(view.gameObject);
            }

            _shopItems.Clear();

            foreach (UpgradeItemView view in _upgradeItems)
            {
                view.Click -= OnUpgradeItemClick;
                Destroy(view.gameObject);
            }

            _upgradeItems.Clear();
            _selectedView = null;
        }
    }
}
