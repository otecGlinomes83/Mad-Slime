using System;
using System.Collections.Generic;
using Game;
using Roulette;
using TMPro;
using Upgrades;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace Skins
{
    public sealed class ShopPanel : MonoBehaviour
    {
        [SerializeField] private Transform _itemsParent;
        [SerializeField] private Transform _upgradesParent;
        [SerializeField] private Transform _rouletteParent;
        [SerializeField] private ShopItemViewFactory _factory;
        [SerializeField] private UpgradeItemViewFactory _upgradeFactory;
        [SerializeField] private TMP_Text _moneyText;
        [SerializeField] private Button _upgradesTabButton;
        [SerializeField] private Button _skinsTabButton;
        [SerializeField] private Button _allSkinsTabButton;
        [SerializeField] private RouletteView _rouletteView;

        private readonly List<ShopItemView> _shopItems = new List<ShopItemView>();
        private readonly List<UpgradeItemView> _upgradeItems = new List<UpgradeItemView>();

        private Wallet _wallet;
        private PlayerUpgrades _upgrades;
        private PlayerProgress _progress;
        private readonly List<SkinItem> _skinItems = new List<SkinItem>();
        private readonly List<SkinItem> _exclusiveSkins = new List<SkinItem>();

        private ShopItemView _selectedView;

        public event Action<ShopItemView> ViewSelected;

        public ShopItemView SelectedView => _selectedView;

        [Inject]
        public void Construct(Wallet wallet, PlayerUpgrades upgrades, PlayerProgress progress)
        {
            _wallet = wallet;
            _upgrades = upgrades;
            _progress = progress;
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

            if (_progress == null)
            {
                throw new InvalidOperationException(
                    $"{name}: PlayerProgress was not injected. Check that ProjectLifetimeScope registers PlayerProgress and ShopLifetimeScope registers ShopPanel.");
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

            if (_rouletteParent == null)
            {
                throw new InvalidOperationException(
                    $"{name}: RouletteParent is not assigned. Drag a Transform into the _rouletteParent field.");
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

            if (_upgradesTabButton == null || _skinsTabButton == null || _allSkinsTabButton == null)
            {
                throw new InvalidOperationException(
                    $"{name}: a tab button is not assigned. Drag the Upgrades, Skins and All Skins tab Buttons into the fields.");
            }

            if (_rouletteView == null)
            {
                throw new InvalidOperationException(
                    $"{name}: RouletteView is not assigned. Drag the RouletteView component into the _rouletteView field.");
            }

            FillSkinList(skinItems);
            FillSkinList(exclusiveSkins, isExclusive: true);

            _wallet.BalanceChanged += OnBalanceChanged;
            _upgradesTabButton.onClick.AddListener(ShowUpgradesTab);
            _skinsTabButton.onClick.AddListener(ShowSkinsTab);
            _allSkinsTabButton.onClick.AddListener(ShowAllSkinsTab);

            _rouletteView.Initialize(RouletteView.Mode.Skins, _skinItems);

            OnBalanceChanged(_wallet.Balance, _wallet.Balance);
        }

        public void ShowUpgradesTab()
        {
            if (CanSwitchTab() == false)
            {
                return;
            }

            SetPageActive(_upgradesParent, _itemsParent, _rouletteParent);
            PopulateUpgrades();
        }

        public void ShowSkinsTab()
        {
            if (CanSwitchTab() == false)
            {
                return;
            }

            SetPageActive(_rouletteParent, _itemsParent, _upgradesParent);
        }

        public void ShowAllSkinsTab()
        {
            if (CanSwitchTab() == false)
            {
                return;
            }

            SetPageActive(_itemsParent, _upgradesParent, _rouletteParent);
            PopulateSkins();
        }

        private bool CanSwitchTab()
        {
            return _rouletteView == null || _rouletteView.IsSpinning == false;
        }

        private static void SetPageActive(Transform activePage, Transform pageA, Transform pageB)
        {
            activePage.gameObject.SetActive(true);
            pageA.gameObject.SetActive(false);
            pageB.gameObject.SetActive(false);
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

        private bool IsOpen(SkinItem item)
        {
            return _progress.OpenSkins.Contains(item.SkinType);
        }

        private bool IsSelected(SkinItem item)
        {
            return _progress.SelectedSkin == item.SkinType;
        }

        private void SelectPersist(SkinItem item)
        {
            _progress.SelectedSkin = item.SkinType;
            _progress.Save();
        }

        private void OnBalanceChanged(int previousBalance, int currentBalance)
        {
            _moneyText.text = currentBalance.ToString();
        }

        private void OnDisable()
        {
            if (_upgradesTabButton != null)
            {
                _upgradesTabButton.onClick.RemoveListener(ShowUpgradesTab);
            }

            if (_skinsTabButton != null)
            {
                _skinsTabButton.onClick.RemoveListener(ShowSkinsTab);
            }

            if (_allSkinsTabButton != null)
            {
                _allSkinsTabButton.onClick.RemoveListener(ShowAllSkinsTab);
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
