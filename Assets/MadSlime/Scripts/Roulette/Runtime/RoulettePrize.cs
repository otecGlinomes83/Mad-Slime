using Skins;

namespace Roulette
{
    public class RoulettePrize
    {
        private SkinItem _skin;
        private int _coins;

        public SkinItem Skin => _skin;
        public int Coins => _coins;

        public RoulettePrize(SkinItem skin, int coins)
        {
            _skin = skin;
            _coins = coins;
        }
    }
}
