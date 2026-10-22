using System;
using System.Collections.Generic;
using Saves;
using Skins;

namespace Roulette
{
    public class SkinShowcase
    {
        private const float FullChance = 100f;

        private RouletteConfig _config;
        private RouletteProbabilityRules _probabilityRules;
        private ISkinStorage _storage;
        private SkinInventory _inventory;
        private IReadOnlyList<SkinItem> _catalog;

        public SkinShowcase(RouletteConfig config, ISkinStorage storage, SkinInventory inventory,
            IReadOnlyList<SkinItem> catalog)
        {
            _config = config;
            _probabilityRules = new RouletteProbabilityRules(config);
            _storage = storage;
            _inventory = inventory;
            _catalog = catalog;
        }

        public void Refresh()
        {
            List<string> existing = _storage.ReadShowcaseSkinIds();
            List<string> formed = new List<string>();
            AppendQuota(formed, existing, SkinRarity.Legendary, _probabilityRules.GetShowcaseQuota(SkinRarity.Legendary));
            AppendQuota(formed, existing, SkinRarity.Epic, _probabilityRules.GetShowcaseQuota(SkinRarity.Epic));
            AppendQuota(formed, existing, SkinRarity.Rare, _probabilityRules.GetShowcaseQuota(SkinRarity.Rare));

            if (formed.Count == 0)
            {
                int quota = _probabilityRules.GetShowcaseQuota(SkinRarity.Legendary)
                    + _probabilityRules.GetShowcaseQuota(SkinRarity.Epic) + _probabilityRules.GetShowcaseQuota(SkinRarity.Rare);
                AppendQuota(formed, existing, SkinRarity.Common, quota);
            }

            if (Matches(existing, formed) == false)
            {
                _storage.SetShowcaseSkinIds(formed);
            }
        }

        public bool HasAvailableSkins()
        {
            for (int i = 0; i < _catalog.Count; i++)
            {
                SkinItem skin = _catalog[i];

                if (IsExclusive(skin) == false && _inventory.IsOpen(skin.Id) == false
                    && _probabilityRules.GetSkinDropChance(skin.Rarity) > 0f)
                {
                    return true;
                }
            }

            return false;
        }

        public List<RouletteSlot> CreateSlots(int slotCount)
        {
            Refresh();
            List<string> ids = _storage.ReadShowcaseSkinIds();
            List<SkinItem> skins = new List<SkinItem>();

            for (int i = 0; i < ids.Count; i++)
            {
                SkinItem skin = Find(ids[i]);

                if (skin != null && skins.Contains(skin) == false)
                {
                    skins.Add(skin);
                }
            }

            EnsureRarityRepresentation(skins, slotCount);

            for (int i = 0; i < _catalog.Count && skins.Count < slotCount; i++)
            {
                SkinItem skin = _catalog[i];

                if (IsExclusive(skin) == false && _inventory.IsOpen(skin.Id) == false
                    && _probabilityRules.GetSkinDropChance(skin.Rarity) > 0f && skins.Contains(skin) == false)
                {
                    skins.Add(skin);
                }
            }

            if (skins.Count == 0)
            {
                for (int i = 0; i < _catalog.Count; i++)
                {
                    if (IsExclusive(_catalog[i]) == false)
                    {
                        skins.Add(_catalog[i]);
                    }
                }

                if (skins.Count == 0)
                {
                    throw new InvalidOperationException("Skin roulette requires ordinary skins in its catalog.");
                }
            }

            if (ids.Count > slotCount)
            {
                throw new InvalidOperationException("Skin showcase exceeds wheel capacity.");
            }

            List<SkinItem> entries = new List<SkinItem>();
            List<SkinItem> remaining = new List<SkinItem>(skins);

            while (entries.Count < slotCount)
            {
                int pickIndex = UnityEngine.Random.Range(0, remaining.Count);
                entries.Add(remaining[pickIndex]);
                remaining.RemoveAt(pickIndex);

                if (remaining.Count == 0)
                {
                    remaining.AddRange(skins);
                }
            }

            float availableChance = 0f;
            HashSet<SkinRarity> availableRarities = new HashSet<SkinRarity>();

            for (int i = 0; i < entries.Count; i++)
            {
                if (availableRarities.Add(entries[i].Rarity) == true)
                {
                    availableChance += _probabilityRules.GetSkinDropChance(entries[i].Rarity);
                }
            }

            List<RouletteSlot> slots = new List<RouletteSlot>();

            for (int i = 0; i < entries.Count; i++)
            {
                SkinItem skin = entries[i];
                int raritySlots = CountRarity(entries, skin.Rarity);
                float weight = 0f;

                if (availableChance > 0f)
                {
                    weight = FullChance * _probabilityRules.GetSkinDropChance(skin.Rarity) / availableChance / raritySlots;
                }
                slots.Add(new RouletteSlot(skin, 0, weight, false));
            }

            return slots;
        }

        private void EnsureRarityRepresentation(List<SkinItem> skins, int slotCount)
        {
            for (int rarityIndex = 0; rarityIndex <= (int)SkinRarity.Legendary; rarityIndex++)
            {
                SkinRarity rarity = (SkinRarity)rarityIndex;

                if (_probabilityRules.GetSkinDropChance(rarity) <= 0f || CountRarity(skins, rarity) > 0)
                {
                    continue;
                }

                SkinItem candidate = FindAvailable(rarity);

                if (candidate == null)
                {
                    continue;
                }

                if (skins.Count >= slotCount)
                {
                    RemoveRepeatedRarity(skins);
                }

                skins.Add(candidate);
            }
        }

        private SkinItem FindAvailable(SkinRarity rarity)
        {
            for (int i = 0; i < _catalog.Count; i++)
            {
                SkinItem skin = _catalog[i];

                if (skin.Rarity == rarity && IsExclusive(skin) == false && _inventory.IsOpen(skin.Id) == false)
                {
                    return skin;
                }
            }

            return null;
        }

        private void RemoveRepeatedRarity(List<SkinItem> skins)
        {
            for (int i = skins.Count - 1; i >= 0; i--)
            {
                if (CountRarity(skins, skins[i].Rarity) > 1)
                {
                    skins.RemoveAt(i);
                    return;
                }
            }

            throw new InvalidOperationException("Skin roulette needs at least one sector for every available rarity with a "
                + "positive chance.");
        }

        private void AppendQuota(List<string> formed, List<string> existing, SkinRarity rarity, int quota)
        {
            int count = 0;

            for (int i = 0; i < existing.Count && count < quota; i++)
            {
                SkinItem skin = Find(existing[i]);

                if (skin == null || skin.Rarity != rarity || _inventory.IsOpen(skin.Id) == true
                    || IsExclusive(skin) == true || formed.Contains(skin.Id) == true)
                {
                    continue;
                }

                formed.Add(skin.Id);
                count++;
            }

            List<SkinItem> candidates = new List<SkinItem>();

            for (int i = 0; i < _catalog.Count; i++)
            {
                SkinItem skin = _catalog[i];

                if (skin.Rarity == rarity && _inventory.IsOpen(skin.Id) == false
                    && IsExclusive(skin) == false && formed.Contains(skin.Id) == false)
                {
                    candidates.Add(skin);
                }
            }

            while (count < quota && candidates.Count > 0)
            {
                int pickIndex = UnityEngine.Random.Range(0, candidates.Count);
                formed.Add(candidates[pickIndex].Id);
                candidates.RemoveAt(pickIndex);
                count++;
            }
        }

        private SkinItem Find(string id)
        {
            for (int i = 0; i < _catalog.Count; i++)
            {
                if (_catalog[i].Id == id)
                {
                    return _catalog[i];
                }
            }

            return null;
        }

        private bool IsExclusive(SkinItem skin)
        {
            for (int i = 0; i < _config.ExclusiveSkins.Count; i++)
            {
                if (_config.ExclusiveSkins[i].Id == skin.Id)
                {
                    return true;
                }
            }

            return false;
        }

        private int CountRarity(List<SkinItem> entries, SkinRarity rarity)
        {
            int count = 0;

            for (int i = 0; i < entries.Count; i++)
            {
                if (entries[i].Rarity == rarity)
                {
                    count++;
                }
            }

            return count;
        }

        private bool Matches(List<string> existing, List<string> formed)
        {
            if (existing.Count != formed.Count)
            {
                return false;
            }

            for (int i = 0; i < existing.Count; i++)
            {
                if (existing[i] != formed[i])
                {
                    return false;
                }
            }

            return true;
        }
    }
}
