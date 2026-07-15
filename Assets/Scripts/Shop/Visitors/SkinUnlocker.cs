using System;
using Game;
using YG;

namespace Skins
{
    public sealed class SkinUnlocker : ISkinVisitor
    {
        private readonly Wallet _wallet;

        public SkinUnlocker(Wallet wallet)
        {
            if (wallet == null)
            {
                throw new ArgumentNullException(nameof(wallet));
            }

            _wallet = wallet;
        }

        public bool Result { get; private set; }

        public void Visit(SkinItem item)
        {
            if (_wallet.Balance < item.Price)
            {
                Result = false;
                return;
            }

            _wallet.Spend(item.Price);
            YG2.saves._openSkins.Add(item.SkinType);
            YG2.SaveProgress();

            Result = true;
        }
    }
}