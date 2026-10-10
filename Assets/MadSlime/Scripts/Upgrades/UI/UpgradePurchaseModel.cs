using Game;
using System;
using Upgrades;

namespace Shop
{
    public class UpgradePurchaseModel
    {
        private PlayerUpgrades _upgrades;
        private Wallet _wallet;
        private Action<Action> _transaction;

        public UpgradePurchaseModel(PlayerUpgrades upgrades, Wallet wallet, Action<Action> transaction)
        {
            _upgrades = upgrades;
            _wallet = wallet;
            _transaction = transaction;
        }

        public bool CanPurchase(UpgradeType type)
        {
            return _upgrades.IsMaxed(type) == false && _wallet.Balance >= _upgrades.GetNextCost(type);
        }

        public bool CanPurchase(PerkType type)
        {
            return _upgrades.IsPerkPurchased(type) == false && _wallet.Balance >= _upgrades.GetPerkCost(type);
        }

        public bool Purchase(UpgradeType type)
        {
            if (CanPurchase(type) == false)
            {
                return false;
            }

            int cost = _upgrades.GetNextCost(type);

            _transaction.Invoke(() =>
            {
                if (cost > 0)
                {
                    _wallet.Spend(cost);
                }

                _upgrades.PurchaseStepped(type);
            });
            return true;
        }

        public bool Purchase(PerkType type)
        {
            if (CanPurchase(type) == false)
            {
                return false;
            }

            int cost = _upgrades.GetPerkCost(type);

            _transaction.Invoke(() =>
            {
                if (cost > 0)
                {
                    _wallet.Spend(cost);
                }

                _upgrades.PurchasePerk(type);
            });
            return true;
        }
    }
}
