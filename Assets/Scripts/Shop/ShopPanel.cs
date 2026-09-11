using System;
using System.Collections.Generic;
using Game;
using TMPro;
using UnityEngine;
using YG;

namespace Skins
{
    public sealed class ShopPanel : MonoBehaviour
    {
        [SerializeField] private Transform _itemsParent;
        [SerializeField] private ShopItemViewFactory _factory;
        [SerializeField] private TMP_Text _moneyText;

        private readonly List<ShopItemView> _shopItems = new List<ShopItemView>();
        private Wallet _wallet;

        private ShopItemView _selectedView;

        public event Action<ShopItemView> ViewSelected;

        public ShopItemView SelectedView => _selectedView;

        public void Initialize(Wallet wallet)
        {
            if (wallet == null)
            {
                throw new ArgumentNullException(nameof(wallet));
            }

            if (_itemsParent == null)
            {
                throw new InvalidOperationException(
                    $"{name}: ItemsParent is not assigned. Drag a Transform into the _itemsParent field.");
            }

            if (_factory == null)
            {
                throw new InvalidOperationException(
                    $"{name}: Factory is not assigned. Drag a ShopItemViewFactory component into the _factory field.");
            }

            if (_moneyText == null)
            {
                throw new InvalidOperationException(
                    $"{name}: MoneyText is not assigned. Drag a TMP_Text into the _moneyText field.");
            }

            if (_wallet != null)
            {
                _wallet.BalanceChanged -= OnBalanceChanged;
            }

            _wallet = wallet;
            _wallet.BalanceChanged += OnBalanceChanged;
            OnBalanceChanged(_wallet.Balance, _wallet.Balance);
        }

        public void Show(IEnumerable<SkinItem> skinItems)
        {
            Clear();

            foreach (SkinItem item in skinItems)
            {
                ShopItemView view = _factory.Get(item, _itemsParent);
                view.Click += OnItemClick;

                if (IsOpen(item) == true)
                {
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
                SelectPersist(view.SkinItem);
                return;
            }

            if (TryUnlock(view.SkinItem) == false)
            {
                return;
            }

            ApplySelection(view);
            view.Unlock();
            SelectPersist(view.SkinItem);
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

        private bool TryUnlock(SkinItem item)
        {
            if (IsOpen(item) == true)
            {
                return true;
            }

            if (_wallet.Balance < item.Price)
            {
                return false;
            }

            if (item.Price > 0)
            {
                _wallet.Spend(item.Price);
            }

            YG2.saves._openSkins.Add(item.SkinType);

            if (YG2.isSDKEnabled == true)
            {
                YG2.SaveProgress();
            }

            return true;
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