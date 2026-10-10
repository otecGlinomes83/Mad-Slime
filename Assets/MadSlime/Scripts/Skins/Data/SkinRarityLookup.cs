using System;

namespace Skins
{
    public class SkinRarityLookup
    {
        private SkinRarityTable _table;

        public SkinRarityLookup(SkinRarityTable table)
        {
            if (table == null)
            {
                throw new ArgumentNullException(nameof(table));
            }

            _table = table;
        }

        public RaritySettings Get(SkinRarity rarity)
        {
            for (int i = 0; i < _table.Settings.Count; i++)
            {
                RaritySettings settings = _table.Settings[i];

                if (settings.Rarity == rarity)
                {
                    return settings;
                }
            }

            throw new InvalidOperationException($"{_table.name}: missing rarity '{rarity}'.");
        }
    }
}
