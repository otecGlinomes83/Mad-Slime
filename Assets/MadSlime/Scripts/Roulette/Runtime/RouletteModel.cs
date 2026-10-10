using System;
using System.Collections.Generic;
using Game;
using Skins;

namespace Roulette
{
    public class RouletteModel
    {
        private RouletteConfig _config;
        private Func<List<RouletteSlot>> _createSlots;
        private RouletteAccess _access;
        private Func<RouletteSlot, RoulettePrize> _resolvePrize;
        private LootRoller _roller;
        private SkinInventory _inventory;
        private Wallet _wallet;
        private Action<Action> _transaction;
        private Action _refreshShowcase;
        private List<RouletteSlot> _slots;
        private bool _advertisementGranted;
        private RouletteOutcome _outcome;

        public RouletteConfig Config => _config;
        public IReadOnlyList<RouletteSlot> Slots => _slots;
        public bool HasAdvertising => _access.HasAdvertising;

        public RouletteModel(RouletteConfig config, Func<List<RouletteSlot>> createSlots, RouletteAccess access,
            Func<RouletteSlot, RoulettePrize> resolvePrize, LootRoller roller, SkinInventory inventory,
            Wallet wallet, Action<Action> transaction, Action refreshShowcase)
        {
            _config = config;
            _createSlots = createSlots;
            _access = access;
            _resolvePrize = resolvePrize;
            _roller = roller;
            _inventory = inventory;
            _wallet = wallet;
            _transaction = transaction;
            _refreshShowcase = refreshShowcase;
        }

        public void RefreshSlots()
        {
            _slots = _createSlots();
        }

        public int GetPrice()
        {
            return _access.GetPrice();
        }

        public int GetFreeRemainSeconds()
        {
            return _access.GetFreeRemainSeconds();
        }

        public int GetAdSpinsLeft()
        {
            return _access.GetAdSpinsLeft();
        }

        public bool CanSpin(bool advertisementGranted)
        {
            if (_slots == null || _slots.Count == 0)
            {
                return false;
            }

            return _access.CanSpin(advertisementGranted);
        }

        public bool Spin(bool advertisementGranted, out RouletteOutcome outcome)
        {
            outcome = null;

            if (CanSpin(advertisementGranted) == false)
            {
                return false;
            }

            _advertisementGranted = advertisementGranted;
            _transaction(ExecuteSpin);
            outcome = _outcome;
            _outcome = null;
            return true;
        }

        private void ExecuteSpin()
        {
            _access.Pay(_advertisementGranted);
            int slotIndex = _roller.Roll(_slots);
            RoulettePrize prize = _resolvePrize(_slots[slotIndex]);

            if (prize.Skin != null)
            {
                if (_inventory.Grant(prize.Skin.Id) == false)
                {
                    prize = new RoulettePrize(null, _config.DuplicateCoinsCompensation);
                }
            }

            if (prize.Coins > 0)
            {
                _wallet.Add(prize.Coins);
            }

            _refreshShowcase?.Invoke();
            _outcome = new RouletteOutcome(slotIndex, prize);
        }
    }
}
