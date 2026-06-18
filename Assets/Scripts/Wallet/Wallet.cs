using System;

namespace Wallet
{
    public class Wallet
    {
        public event Action<int,int> BalanceChanged;
        public int Balance { get; private set; }
        
        public void AddMoney(int amount)
        {
            if (amount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(amount), "Amount cannot be negative");
            }
            
            int previous =  Balance;
            Balance += amount;
            
            BalanceChanged?.Invoke(previous, Balance);
        }
        
        public void Spend(int amount)
        {
            if (amount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(amount), "Amount cannot be negative");
            }

            if (amount > Balance)
            {
                throw new ArgumentOutOfRangeException(nameof(amount), "Amount cannot be greater than Balance");
            }
            
            int previous =  Balance;
            Balance -= amount;
            
            BalanceChanged?.Invoke(previous, Balance);
        }
    }
}