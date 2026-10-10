using System;
using System.Collections.Generic;

namespace Roulette
{
    public class LootRoller
    {
        public int Roll(IReadOnlyList<RouletteSlot> slots)
        {
            float total = 0f;

            for (int i = 0; i < slots.Count; i++)
            {
                total += slots[i].Weight;
            }

            if (total <= 0f)
            {
                throw new InvalidOperationException("Roulette slots must have a positive total weight.");
            }

            float roll = UnityEngine.Random.Range(0f, total);
            int lastWeightedIndex = -1;

            for (int i = 0; i < slots.Count; i++)
            {
                if (slots[i].Weight <= 0f)
                {
                    continue;
                }

                lastWeightedIndex = i;
                roll -= slots[i].Weight;

                if (roll < 0f)
                {
                    return i;
                }
            }

            return lastWeightedIndex;
        }
    }
}
