using System;
using UnityEngine;
using YG;

namespace Game
{
    public sealed class Wallet : MonoBehaviour
    {
        public event Action<int, int> BalanceChanged;

        public int Balance => YG2.saves.Balance;

        public void Add(int amount)
        {
            if (amount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(amount),
                    "Wallet.Add requires a positive amount.");
            }

            int previousBalance = YG2.saves.Balance;
            YG2.saves.Balance = previousBalance + amount;
            YG2.SaveProgress();

            BalanceChanged?.Invoke(previousBalance, YG2.saves.Balance);
        }

        public void Spend(int amount)
        {
            if (amount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(amount),
                    "Wallet.Spend requires a positive amount.");
            }

            if (YG2.saves.Balance < amount)
            {
                throw new InvalidOperationException(
                    $"Wallet.Spend failed: balance {YG2.saves.Balance} is less than required {amount}.");
            }

            int previousBalance = YG2.saves.Balance;
            YG2.saves.Balance = previousBalance - amount;
            YG2.SaveProgress();

            BalanceChanged?.Invoke(previousBalance, YG2.saves.Balance);
        }
    }
}