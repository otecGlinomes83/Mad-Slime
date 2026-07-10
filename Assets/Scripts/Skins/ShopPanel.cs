using System;
using System.Collections.Generic;
using Game;
using UnityEngine;
using YG;

public class ShopPanel : MonoBehaviour
{
    [SerializeField] private Transform _itemsParent;
    [SerializeField] private SkinItemViewFactory _factory;
    [SerializeField] private TMPro.TMP_Text _moneyText;

    private readonly List<SkinItemView> _shopItems = new List<SkinItemView>();
    private Wallet _wallet;
    private SkinItemView _selectedView;

    public event Action<SkinItemView> ViewSelected;
    
    public void Initialize(Wallet wallet)
    {
        if (wallet == null)
        {
            throw new System.ArgumentNullException(nameof(wallet));
        }

        _wallet = wallet;

        _wallet.BalanceChanged += OnBalanceChanged;
        UpdateMoneyText(_wallet.Balance);
    }

    public void Show(IEnumerable<SkinItem> skinItems)
    {
        Clear();

        foreach (SkinItem item in skinItems)
        {
            if (item == null)
            {
                continue;
            }

            SkinItemView view = _factory.Get(item, _itemsParent);
            view.Click += OnItemClick;

            bool isOwned = YG2.saves._openSkins.Contains(item.SkinType);

            if (isOwned)
            {
                view.Unlock();

                if (YG2.saves._selectedSkin == item.SkinType)
                {
                    view.Select();
                    _selectedView = view;
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

            view.UnHighlight();
            _shopItems.Add(view);
        }
    }

    private void OnItemClick(SkinItemView view)
    {
        ViewSelected?.Invoke(view);
        
        if (view.IsLock)
        {
            if (_wallet.Balance < view.Price)
            {
                return;
            }

            _wallet.Spend(view.Price);

            YG2.saves._openSkins.Add(view.SkinItem.SkinType);
            YG2.SaveProgress();

            view.Unlock();
            ApplySelection(view);
        }
        else
        {
            ApplySelection(view);
        }
    }

    private void ApplySelection(SkinItemView view)
    {
        if (_selectedView != null && _selectedView != view)
        {
            _selectedView.UnSelect();
        }

        view.Select();
        _selectedView = view;

        YG2.saves._selectedSkin = view.SkinItem.SkinType;
        YG2.SaveProgress();
    }

    private void OnBalanceChanged(int previousBalance, int currentBalance)
    {
        UpdateMoneyText(currentBalance);
    }

    private void UpdateMoneyText(int balance)
    {
            _moneyText.text = balance.ToString();
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
        foreach (SkinItemView view in _shopItems)
        {
            view.Click -= OnItemClick;
            Destroy(view.gameObject);
        }

        _shopItems.Clear();
        _selectedView = null;
    }
}