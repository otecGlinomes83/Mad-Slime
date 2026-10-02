using System;
using System.Collections.Generic;
using Game;
using Skins;
using UnityEngine;
using VContainer;
using Random = UnityEngine.Random;

namespace Roulette
{
    public sealed class RouletteService : MonoBehaviour
    {
        private static readonly SkinRarity[] RarityOrder =
        {
            SkinRarity.Common,
            SkinRarity.Rare,
            SkinRarity.Epic,
            SkinRarity.Legendary
        };

        [SerializeField] private RouletteConfig _config;

        private PlayerProgress _progress;
        private Wallet _wallet;

        [Inject]
        public void Construct(PlayerProgress progress, Wallet wallet)
        {
            _progress = progress;
            _wallet = wallet;
        }

        private void Awake()
        {
            if (_config == null)
            {
                throw new InvalidOperationException(
                    $"{name}: RouletteConfig is not assigned. Drag the RouletteConfig asset into the _config field.");
            }

            if (_progress == null)
            {
                throw new InvalidOperationException(
                    $"{name}: PlayerProgress was not injected. Check that ProjectLifetimeScope registers PlayerProgress.");
            }

            if (_wallet == null)
            {
                throw new InvalidOperationException(
                    $"{name}: Wallet was not injected. Check that the scene LifetimeScope (Menu or Shop) registers a Wallet and RouletteService.");
            }

            if (_config.RarityTable == null)
            {
                throw new InvalidOperationException(
                    $"{name}: RouletteConfig '{_config.name}' has no SkinRarityTable assigned.");
            }

            for (int i = 0; i < RarityOrder.Length; i++)
            {
                RaritySettings settings = _config.RarityTable.Get(RarityOrder[i]);

                if (settings.DropWeight <= 0f)
                {
                    throw new InvalidOperationException(
                        $"{name}: Rarity '{RarityOrder[i]}' in '{_config.RarityTable.name}' has a non-positive drop weight.");
                }
            }

            for (int i = 0; i < _config.Sectors.Count; i++)
            {
                RouletteSector sector = _config.Sectors[i];

                if (sector.RewardType == RouletteSector.RewardKind.Skin && sector.Skin == null)
                {
                    throw new InvalidOperationException(
                        $"{name}: RouletteConfig '{_config.name}' sector {i} is of type Skin but has no SkinItem assigned.");
                }

                if (sector.RewardType == RouletteSector.RewardKind.Coins && sector.Coins <= 0)
                {
                    throw new InvalidOperationException(
                        $"{name}: RouletteConfig '{_config.name}' sector {i} is of type Coins but pays zero coins.");
                }
            }

            float totalWeight = 0f;

            for (int i = 0; i < _config.Sectors.Count; i++)
            {
                totalWeight += _config.Sectors[i].Weight;
            }

            if (totalWeight <= 0f)
            {
                throw new InvalidOperationException(
                    $"{name}: RouletteConfig '{_config.name}' sectors have zero total weight — nothing can drop.");
            }

            if (_config.DuplicateCoinsCompensation <= 0)
            {
                throw new InvalidOperationException(
                    $"{name}: RouletteConfig '{_config.name}' has DuplicateCoinsCompensation <= 0. " +
                    "Wallet.Add requires a positive amount.");
            }
        }

        public RouletteConfig Config => _config;

        public int GetSkinSpinCost()
        {
            // Старт с минимального порога, дальше — шаг за крутку без сброса;
            // максимальный порог (если задан) ограничивает сверху.
            int cost = _config.SkinSpinMinCost + _config.SkinSpinCostStep * _progress.SkinSpinCount;

            if (_config.SkinSpinMaxCost > 0 && cost > _config.SkinSpinMaxCost)
            {
                cost = _config.SkinSpinMaxCost;
            }

            return cost;
        }

        public bool CanSpinFree(long nowUnixTime)
        {
            return nowUnixTime - _progress.LastFreeSpinUnixTime >= _config.FreeSpinCooldownSeconds;
        }

        public int GetFreeSpinRemainSeconds(long nowUnixTime)
        {
            long elapsed = nowUnixTime - _progress.LastFreeSpinUnixTime;
            int remain = _config.FreeSpinCooldownSeconds - (int)elapsed;

            return Mathf.Max(0, remain);
        }

        public bool CanSpinForAd(long nowUnixTime)
        {
            return CountAdSpinsInWindow(nowUnixTime) < _config.AdSpinsPerWindow;
        }

        public int GetAdSpinsLeft(long nowUnixTime)
        {
            return _config.AdSpinsPerWindow - CountAdSpinsInWindow(nowUnixTime);
        }

        public void RegisterFreeSpin(long nowUnixTime)
        {
            _progress.LastFreeSpinUnixTime = nowUnixTime;
            _progress.Save();
        }

        public void RegisterAdSpin(long nowUnixTime)
        {
            PruneAdSpins(nowUnixTime);

            if (_progress.RouletteAdSpinTimes.Count >= _config.AdSpinsPerWindow)
            {
                throw new InvalidOperationException(
                    "RouletteService: ad spin limit for the current window is already reached.");
            }

            _progress.RouletteAdSpinTimes.Add(nowUnixTime);
            _progress.Save();
        }

        public bool CanSpinSkinsForCoins()
        {
            return _wallet.Balance >= GetSkinSpinCost();
        }

        public void PaySkinSpin()
        {
            int cost = GetSkinSpinCost();

            if (_wallet.Balance < cost)
            {
                throw new InvalidOperationException(
                    $"RouletteService: balance {_wallet.Balance} is less than the skin spin cost {cost}.");
            }

            _wallet.Spend(cost);
            _progress.SkinSpinCount++;
            _progress.Save();
        }

        public void GrantCoins(int amount)
        {
            _wallet.Add(amount);
        }

        public List<SkinItem> CollectAvailableSkinPool(IEnumerable<SkinItem> source)
        {
            List<SkinItem> pool = new List<SkinItem>();

            foreach (SkinItem item in source)
            {
                if (item == null)
                {
                    continue;
                }

                if (IsSkinOpen(item) == false)
                {
                    pool.Add(item);
                }
            }

            return pool;
        }

        public bool IsSkinOpen(SkinItem item)
        {
            return _progress.OpenSkins.Contains(item.SkinType);
        }

        public void GrantSkin(SkinItem item)
        {
            if (IsSkinOpen(item) == true)
            {
                return;
            }

            _progress.OpenSkins.Add(item.SkinType);
            _progress.Save();
        }

        public int PickMainSectorIndex()
        {
            IReadOnlyList<RouletteSector> sectors = _config.Sectors;
            float totalWeight = 0f;

            for (int i = 0; i < sectors.Count; i++)
            {
                totalWeight += sectors[i].Weight;
            }

            float roll = Random.Range(0f, totalWeight);

            for (int i = 0; i < sectors.Count; i++)
            {
                roll -= sectors[i].Weight;

                if (roll <= 0f)
                {
                    return i;
                }
            }

            return sectors.Count - 1;
        }

        public int PickSkinIndex(List<SkinItem> pool)
        {
            if (pool == null || pool.Count == 0)
            {
                throw new ArgumentException(
                    $"{name}: PickSkinIndex requires a non-empty skin pool.");
            }

            SkinRarity[] tiers = new SkinRarity[RarityOrder.Length];
            float[] tierWeights = new float[RarityOrder.Length];
            float totalWeight = 0f;
            int tierCount = 0;

            for (int i = 0; i < RarityOrder.Length; i++)
            {
                bool hasTier = HasSkinOfRarity(pool, RarityOrder[i]);

                if (hasTier == false)
                {
                    continue;
                }

                float weight = _config.RarityTable.Get(RarityOrder[i]).DropWeight;

                tiers[tierCount] = RarityOrder[i];
                tierWeights[tierCount] = weight;
                totalWeight += weight;
                tierCount++;
            }

            SkinRarity chosenRarity = RollRarity(tiers, tierWeights, tierCount, totalWeight);

            return PickIndexInRarity(pool, chosenRarity);
        }

        public Color GetRarityColor(SkinRarity rarity)
        {
            return _config.RarityTable.Get(rarity).PlateColor;
        }

        public void SortByRarityAscending(List<SkinItem> skins)
        {
            List<SkinItem> sorted = new List<SkinItem>(skins.Count);

            for (int i = 0; i < RarityOrder.Length; i++)
            {
                for (int j = 0; j < skins.Count; j++)
                {
                    if (skins[j].Rarity == RarityOrder[i])
                    {
                        sorted.Add(skins[j]);
                    }
                }
            }

            skins.Clear();

            for (int i = 0; i < sorted.Count; i++)
            {
                skins.Add(sorted[i]);
            }
        }

        private static bool HasSkinOfRarity(List<SkinItem> pool, SkinRarity rarity)
        {
            for (int i = 0; i < pool.Count; i++)
            {
                if (pool[i].Rarity == rarity)
                {
                    return true;
                }
            }

            return false;
        }

        private static SkinRarity RollRarity(SkinRarity[] tiers, float[] tierWeights, int tierCount, float totalWeight)
        {
            float roll = Random.Range(0f, totalWeight);

            for (int i = 0; i < tierCount; i++)
            {
                roll -= tierWeights[i];

                if (roll <= 0f)
                {
                    return tiers[i];
                }
            }

            return tiers[tierCount - 1];
        }

        private static int PickIndexInRarity(List<SkinItem> pool, SkinRarity rarity)
        {
            int candidateCount = 0;

            for (int i = 0; i < pool.Count; i++)
            {
                if (pool[i].Rarity == rarity)
                {
                    candidateCount++;
                }
            }

            int pick = Random.Range(0, candidateCount);

            for (int i = 0; i < pool.Count; i++)
            {
                if (pool[i].Rarity != rarity)
                {
                    continue;
                }

                if (pick == 0)
                {
                    return i;
                }

                pick--;
            }

            throw new InvalidOperationException(
                $"RouletteService: failed to pick a skin of rarity '{rarity}' from a pool of {pool.Count} skins.");
        }

        public void PruneAdSpins(long nowUnixTime)
        {
            List<long> spinTimes = _progress.RouletteAdSpinTimes;
            long windowStart = nowUnixTime - _config.AdSpinWindowSeconds;

            for (int i = spinTimes.Count - 1; i >= 0; i--)
            {
                if (spinTimes[i] < windowStart)
                {
                    spinTimes.RemoveAt(i);
                }
            }
        }

        private int CountAdSpinsInWindow(long nowUnixTime)
        {
            List<long> spinTimes = _progress.RouletteAdSpinTimes;
            long windowStart = nowUnixTime - _config.AdSpinWindowSeconds;
            int count = 0;

            for (int i = 0; i < spinTimes.Count; i++)
            {
                if (spinTimes[i] >= windowStart)
                {
                    count++;
                }
            }

            return count;
        }
    }
}
