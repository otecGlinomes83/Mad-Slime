using System;
using Skills;

namespace Scriptables
{
    public class TierLookup
    {
        private TierTable _table;

        public TierLookup(TierTable table)
        {
            if (table == null)
            {
                throw new ArgumentNullException(nameof(table));
            }

            _table = table;
        }

        public TierEntry Get(SizeTier tier)
        {
            for (int i = 0; i < _table.Entries.Count; i++)
            {
                TierEntry entry = _table.Entries[i];

                if (entry.Tier == tier)
                {
                    if (entry.Scale <= 0f || entry.Mass <= 0)
                    {
                        throw new InvalidOperationException($"{_table.name}: tier '{tier}' requires positive scale and mass.");
                    }

                    return entry;
                }
            }

            throw new InvalidOperationException($"{_table.name}: missing tier '{tier}'.");
        }
    }
}
