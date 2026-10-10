using Skins;

namespace Roulette
{
    public class RouletteSlot
    {
        private SkinItem _skin;
        private int _coins;
        private float _weight;
        private bool _isHidden;

        public SkinItem Skin => _skin;
        public int Coins => _coins;
        public float Weight => _weight;
        public bool IsHidden => _isHidden;

        public RouletteSlot(SkinItem skin, int coins, float weight, bool isHidden)
        {
            _skin = skin;
            _coins = coins;
            _weight = weight;
            _isHidden = isHidden;
        }
    }
}
