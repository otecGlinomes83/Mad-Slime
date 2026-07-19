using System;
using System.Collections.Generic;
using Audio;
using Game;
using UnityEngine;

namespace Skins
{
    public class ShopPanel : MonoBehaviour
    {
        [SerializeField] private Transform _itemsParent;
        [SerializeField] private SkinItemViewFactory _factory;
        [SerializeField] private TMPro.TMP_Text _moneyText;
        
        private readonly List<ShopItemView> _shopItems = new List<ShopItemView>();
        private Wallet _wallet;

        private SelectedChecker _selectedChecker;
        private AvailableChecker _availableChecker;
        private SkinUnlocker _skinUnlocker;
        private SkinSelector _skinSelector;

        private ShopItemView _selectedView;

        public event Action<ShopItemView> ViewSelected;

        public ShopItemView SelectedView => _selectedView;

        public void Initialize(Wallet wallet)
        {
            if (wallet == null)
            {
                throw new ArgumentNullException(nameof(wallet));
            }

            _wallet = wallet;
            _wallet.BalanceChanged += OnBalanceChanged;
            OnBalanceChanged(_wallet.Balance, _wallet.Balance);

            _selectedChecker = new SelectedChecker();
            _availableChecker = new AvailableChecker();
            _skinUnlocker = new SkinUnlocker(wallet);
            _skinSelector = new SkinSelector();
        }

        public void Show(IEnumerable<SkinItem> skinItems)
        {
            Clear();

            foreach (SkinItem item in skinItems)
            {
                ShopItemView view = _factory.Get(item, _itemsParent);
                view.Click += OnItemClick;

                _availableChecker.Visit(item);

                if (_availableChecker.Result)
                {
                    view.Unlock();

                    _selectedChecker.Visit(item);

                    if (_selectedChecker.Result)
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
                    view.Lock();
                    view.UnSelect();
                }

                _shopItems.Add(view);
            }
        }

        private void OnItemClick(ShopItemView view)
        {
            ViewSelected?.Invoke(view);

            if (view.IsLock == false)
            {
                ApplySelection(view);
                return;
            }

            _skinUnlocker.Visit(view.SkinItem);

            if (_skinUnlocker.Result == false)
            {
                return;
            }

            ApplySelection(view);
            view.Unlock();
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

            _skinSelector.Visit(view.SkinItem);
        }

        private void OnBalanceChanged(int previousBalance, int currentBalance)
        {
            _moneyText.text = currentBalance.ToString();
        }

        private void OnDisable()
        {
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
                view.Click -= OnItemClick;
                Destroy(view.gameObject);
            }

            _shopItems.Clear();
            _selectedView = null;
        }
    }
}