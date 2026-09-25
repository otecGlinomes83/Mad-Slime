using System;
using System.Collections.Generic;
using Game;
using Skins;
using UnityEngine;
using VContainer;

namespace Roulette
{
    public sealed class RouletteService : MonoBehaviour
    {
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
        }

        public RouletteConfig Config => _config;

        public int MainSpinCost => _config.MainSpinCost;

        public int GetSkinSpinCost()
        {
            return _config.SkinSpinBaseCost + _config.SkinSpinCostStep * _progress.SkinSpinCount;
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
            PruneAdSpins(nowUnixTime);

            return _progress.RouletteAdSpinTimes.Count < _config.AdSpinsPerWindow;
        }

        public int GetAdSpinsLeft(long nowUnixTime)
        {
            PruneAdSpins(nowUnixTime);

            return _config.AdSpinsPerWindow - _progress.RouletteAdSpinTimes.Count;
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

        public bool CanSpinForCoins()
        {
            return _wallet.Balance >= _config.MainSpinCost;
        }

        public void PayMainSpin()
        {
            if (_wallet.Balance < _config.MainSpinCost)
            {
                throw new InvalidOperationException(
                    $"RouletteService: balance {_wallet.Balance} is less than the main spin cost {_config.MainSpinCost}.");
            }

            _wallet.Spend(_config.MainSpinCost);
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

        private void PruneAdSpins(long nowUnixTime)
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
    }
}
