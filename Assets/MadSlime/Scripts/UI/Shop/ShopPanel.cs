using System;
using System.Collections.Generic;
using Game;
using Player;
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
        [SerializeField] private ShopContent _shopContent;
        [SerializeField] private Transform _itemsParent;
        [SerializeField] private Transform _upgradesParent;
        [SerializeField] private GameObject _upgradesPage;
        [SerializeField] private GameObject _allSkinsPage;
        [SerializeField] private GameObject _roulettePage;
        [SerializeField] private ShopItemViewFactory _factory;
        [SerializeField] private UpgradeItemViewFactory _upgradeFactory;
        [SerializeField, Tooltip("Плашка-разделитель перед одноразовыми покупками. Пусто — без разделителя.")]
        private GameObject _perkSeparatorPrefab;
        [SerializeField] private TMP_Text _moneyText;
        [SerializeField] private Button _upgradesTabButton;
        [SerializeField] private Button _rouletteTabButton;
        [SerializeField] private Button _allSkinsTabButton;
        [SerializeField] private RouletteView _rouletteView;

        private readonly List<ShopItemView> _shopItems = new List<ShopItemView>();
        private readonly List<UpgradeItemView> _upgradeItems = new List<UpgradeItemView>();

        private Wallet _wallet;
        private PlayerUpgrades _upgrades;
        private PlayerProgress _progress;
        private RouletteService _rouletteService;
        private ModelPlacer _modelPlacer;
        private readonly List<SkinItem> _skinItems = new List<SkinItem>();
        private readonly List<SkinItem> _exclusiveSkins = new List<SkinItem>();

        private ShopItemView _selectedView;
        private GameObject _perkSeparator;
        private bool _isInitialized;

        public bool IsRouletteSpinning => _rouletteView != null && _rouletteView.IsSpinning;

        [Inject]
        public void Construct(Wallet wallet, PlayerUpgrades upgrades, PlayerProgress progress,
            RouletteService rouletteService, ModelPlacer modelPlacer)
        {
            _wallet = wallet;
            _upgrades = upgrades;
            _progress = progress;
            _rouletteService = rouletteService;
            _modelPlacer = modelPlacer;
        }

        private void OnEnable()
        {
            _progress.Ready += InitializeShop;

            if (_progress.IsReady == true)
            {
                InitializeShop();
            }
        }

        private void InitializeShop()
        {
            if (_isInitialized == true)
            {
                return;
            }

            if (_shopContent == null)
            {
                throw new InvalidOperationException(
                    $"{name}: ShopContent is not assigned. Drag the ShopContent asset into the _shopContent field.");
            }

            Initialize(_shopContent.SkinItems, _rouletteService.Config.ExclusiveSkins);
            ShowEquippedSkin();
            ShowRouletteTab();
        }

        // При входе в магазин превью сразу показывает надетый скин и крутится,
        // как после клика по его плашке.
        private void ShowEquippedSkin()
        {
            SkinItem equipped = FindSkin(_progress.SelectedSkin);

            if (equipped == null)
            {
                throw new InvalidOperationException(
                    $"{name}: the selected skin '{_progress.SelectedSkin}' is missing from ShopContent. " +
                    "Add it to SkinItems so the preview can show the equipped skin.");
            }

            _modelPlacer.SetModel(equipped.Model);
            _modelPlacer.PlayWalk();
        }

        private SkinItem FindSkin(PlayerSkins skinType)
        {
            foreach (SkinItem item in _skinItems)
            {
                if (item != null && item.SkinType == skinType)
                {
                    return item;
                }
            }

            return null;
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

            if (_rouletteService == null)
            {
                throw new InvalidOperationException(
                    $"{name}: RouletteService was not injected. Check that ShopLifetimeScope registers RouletteService and ShopPanel.");
            }

            if (_modelPlacer == null)
            {
                throw new InvalidOperationException(
                    $"{name}: ModelPlacer was not injected. Check that ShopLifetimeScope registers ModelPlacer and ShopPanel.");
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

            if (_upgradesPage == null || _allSkinsPage == null || _roulettePage == null)
            {
                throw new InvalidOperationException(
                    $"{name}: a page is not assigned. Drag the Upgrades, All Skins and Roulette page objects into the fields.");
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

            if (_upgradesTabButton == null || _rouletteTabButton == null || _allSkinsTabButton == null)
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
            _rouletteService.SortByRarityAscending(_skinItems);

            _wallet.BalanceChanged += OnBalanceChanged;
            _upgradesTabButton.onClick.AddListener(ShowUpgradesTab);
            _rouletteTabButton.onClick.AddListener(ShowRouletteTab);
            _allSkinsTabButton.onClick.AddListener(ShowAllSkinsTab);

            _rouletteView.Initialize(RouletteView.Mode.Skins, _skinItems);

            OnBalanceChanged(_wallet.Balance, _wallet.Balance);

            _isInitialized = true;
        }

        public void ShowUpgradesTab()
        {
            if (CanSwitchTab() == false)
            {
                return;
            }

            SetPageActive(_upgradesPage, _allSkinsPage, _roulettePage);
            PopulateUpgrades();
            SelectTabButton(_upgradesTabButton);
        }

        public void ShowRouletteTab()
        {
            if (CanSwitchTab() == false)
            {
                return;
            }

            SetPageActive(_roulettePage, _allSkinsPage, _upgradesPage);
            SelectTabButton(_rouletteTabButton);
        }

        public void ShowAllSkinsTab()
        {
            if (CanSwitchTab() == false)
            {
                return;
            }

            SetPageActive(_allSkinsPage, _upgradesPage, _roulettePage);
            PopulateSkins();
            SelectTabButton(_allSkinsTabButton);
        }

        private bool CanSwitchTab()
        {
            return _rouletteView == null || _rouletteView.IsSpinning == false;
        }

        private static void SetPageActive(GameObject activePage, GameObject pageA, GameObject pageB)
        {
            activePage.SetActive(true);
            pageA.SetActive(false);
            pageB.SetActive(false);
        }

        private static void SelectTabButton(Button tabButton)
        {
            if (tabButton != null && tabButton.TryGetComponent(out ShopTabButton tab) == true)
            {
                tab.Select();
            }
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

                if (isExclusive == true && _exclusiveSkins.Contains(item) == false)
                {
                    _exclusiveSkins.Add(item);
                }

                if (_skinItems.Contains(item) == false)
                {
                    _skinItems.Add(item);
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
                view.SetRarityColor(_rouletteService.GetRarityColor(item.Rarity));

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

            if (_perkSeparatorPrefab != null)
            {
                _perkSeparator = Instantiate(_perkSeparatorPrefab, _upgradesParent);
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
            // Закрытый скин тоже показываем в превью, но не выбираем его:
            // галочка остаётся на надетом скине.
            _modelPlacer.SetModel(view.Model);
            _modelPlacer.PlayWalk();

            if (view.IsLock == true)
            {
                return;
            }

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
            _progress.Ready -= InitializeShop;

            if (_upgradesTabButton != null)
            {
                _upgradesTabButton.onClick.RemoveListener(ShowUpgradesTab);
            }

            if (_rouletteTabButton != null)
            {
                _rouletteTabButton.onClick.RemoveListener(ShowRouletteTab);
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
            // При выгрузке сцены виды бывают уже уничтожены движком — гварды
            // обязательны, иначе MissingReferenceException в OnDisable.
            foreach (ShopItemView view in _shopItems)
            {
                if (view == null)
                {
                    continue;
                }

                view.Click -= OnSkinItemClick;
                Destroy(view.gameObject);
            }

            _shopItems.Clear();

            foreach (UpgradeItemView view in _upgradeItems)
            {
                if (view == null)
                {
                    continue;
                }

                view.Click -= OnUpgradeItemClick;
                Destroy(view.gameObject);
            }

            _upgradeItems.Clear();

            if (_perkSeparator != null)
            {
                Destroy(_perkSeparator);
                _perkSeparator = null;
            }

            _selectedView = null;
        }
    }
}
