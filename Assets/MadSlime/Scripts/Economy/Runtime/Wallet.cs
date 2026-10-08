using System;
using UnityEngine;
using VContainer;

namespace Game
{
    public sealed class Wallet : MonoBehaviour
    {
        private PlayerProgress _progress;

        public event Action<int, int> BalanceChanged;

        public int Balance => _progress.Balance;

        [Inject]
        public void Construct(PlayerProgress progress)
        {
            _progress = progress;
        }

        public void Add(int amount)
        {
            if (amount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(amount),
                    "Wallet.Add requires a positive amount.");
            }

            int previousBalance = _progress.Balance;
            _progress.Balance = previousBalance + amount;
            _progress.Save();

            BalanceChanged?.Invoke(previousBalance, _progress.Balance);
        }

        public void Spend(int amount)
        {
            if (amount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(amount),
                    "Wallet.Spend requires a positive amount.");
            }

            if (_progress.Balance < amount)
            {
                throw new InvalidOperationException(
                    $"Wallet.Spend failed: balance {_progress.Balance} is less than required {amount}.");
            }

            int previousBalance = _progress.Balance;
            _progress.Balance = previousBalance - amount;
            _progress.Save();

            BalanceChanged?.Invoke(previousBalance, _progress.Balance);
        }
    }
}
