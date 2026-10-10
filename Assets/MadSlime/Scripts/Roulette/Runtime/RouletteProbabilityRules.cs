using System;
using Skins;

namespace Roulette
{
    public class RouletteProbabilityRules
    {
        private RouletteConfig _config;

        public RouletteProbabilityRules(RouletteConfig config)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            _config = config;
        }

        public float GetSkinDropChance(SkinRarity rarity)
        {
            switch (rarity)
            {
                case SkinRarity.Rare:
                    return _config.RareDropChance;

                case SkinRarity.Epic:
                    return _config.EpicDropChance;

                case SkinRarity.Legendary:
                    return _config.LegendaryDropChance;

                default:
                    return _config.CommonDropChance;
            }
        }

        public int GetShowcaseQuota(SkinRarity rarity)
        {
            switch (rarity)
            {
                case SkinRarity.Rare:
                    return _config.ShowcaseRareCount;

                case SkinRarity.Epic:
                    return _config.ShowcaseEpicCount;

                case SkinRarity.Legendary:
                    return _config.ShowcaseLegendaryCount;

                default:
                    return 0;
            }
        }

    }
}
