using Saves;
using System;
using UnityEngine;
using VContainer;

namespace Game
{
    public class Wallet : MonoBehaviour
    {
        private IBalanceStorage _balanceStorage;

        public event Action<int, int> BalanceChanged;

        public int Balance => _balanceStorage.Balance;

        [Inject]
        public void Construct(IBalanceStorage balanceStorage)
        {
            _balanceStorage = balanceStorage;
        }

        public void Add(int amount)
        {
            if (amount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(amount),
                    "Wallet.Add requires a positive amount.");
            }

            SetBalance(_balanceStorage.Balance + amount);
        }

        public void Spend(int amount)
        {
            if (amount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(amount),
                    "Wallet.Spend requires a positive amount.");
            }

            if (_balanceStorage.Balance < amount)
            {
                throw new InvalidOperationException(
                    $"Wallet.Spend failed: balance {_balanceStorage.Balance} is less than required {amount}.");
            }

            SetBalance(_balanceStorage.Balance - amount);
        }

        private void SetBalance(int balance)
        {
            int previousBalance = _balanceStorage.Balance;

            _balanceStorage.SetBalance(balance);

            BalanceChanged?.Invoke(previousBalance, balance);
        }
    }
}
