using System;
using System.Collections.Generic;
using Game;
using Skins;
using Shop;
using UnityEditor;
using UnityEngine;

namespace Roulette.Editor
{
    public class RouletteBehaviorChecks
    {
        private RouletteTestStorage _storage;
        private Wallet _wallet;
        private RouletteConfig _config;
        private SkinInventory _inventory;
        private SkinItem _rewardSkin;
        private int _commits;
        private bool _requiresAd;

        [MenuItem("Mad Slime/Checks/Roulette Behavior")]
        public static void Run()
        {
            RouletteBehaviorChecks checks = new RouletteBehaviorChecks();
            checks.Check();
            checks.CheckCommonShowcase();
            Debug.Log("Roulette behavior checks passed: committed reward before presentation, duplicate compensation, access rejection, ad confirmation, and exhausted-rarity Common showcase fallback.");
        }

        private void Check()
        {
            GameObject walletObject = new GameObject("Roulette behavior wallet");

            try
            {
                _storage = new RouletteTestStorage();
                _wallet = walletObject.AddComponent<Wallet>();
                _wallet.Construct(_storage);
                _inventory = new SkinInventory(_storage);
                _config = AssetDatabase.LoadAssetAtPath<RouletteConfig>("Assets/MadSlime/Scriptables/Roulette/RouletteConfig.asset");
                _rewardSkin = AssetDatabase.LoadAssetAtPath<SkinItem>("Assets/MadSlime/Scriptables/Skins/Slime.asset");
                RouletteAccess access = new RouletteAccess(GetPrice, GetZero, GetZero, CanSpin, Pay, true);
                RouletteModel model = new RouletteModel(_config, CreateSlots, access, ResolvePrize,
                    new LootRoller(), _inventory, _wallet, Commit, null);
                model.RefreshSlots();
                RouletteOutcome outcome;
                Require(model.Spin(false, out outcome) == true, "Accessible spin was rejected.");
                Require(_commits == 1 && _inventory.IsOpen(_rewardSkin.Id) == true && _wallet.Balance == 900,
                    "Spin returned before reward and save completed.");
                Require(outcome.SlotIndex == 0 && outcome.Prize.Skin == _rewardSkin, "Spin returned a mismatched outcome.");
                Require(model.Spin(false, out outcome) == true, "Duplicate spin was rejected.");
                Require(outcome.Prize.Skin == null && outcome.Prize.Coins == _config.DuplicateCoinsCompensation,
                    "Duplicate skin did not become compensation.");
                Require(_wallet.Balance == 800 + _config.DuplicateCoinsCompensation && _commits == 2,
                    "Duplicate compensation was not saved before returning.");
                _requiresAd = true;
                int balance = _wallet.Balance;
                Require(model.Spin(false, out outcome) == false, "Unconfirmed advertising accessed a spin.");
                Require(_wallet.Balance == balance && _commits == 2 && outcome == null,
                    "Rejected spin charged currency or saved a reward.");
                Require(model.Spin(true, out outcome) == true && _commits == 3,
                    "Confirmed advertising did not commit a spin.");
                _storage.SetBalance(0);
                _requiresAd = false;
                Require(model.Spin(false, out outcome) == false && _commits == 3,
                    "Insufficient balance accessed a paid spin.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(walletObject);
            }
        }

        private void CheckCommonShowcase()
        {
            ShopContent content = AssetDatabase.LoadAssetAtPath<ShopContent>("Assets/MadSlime/Scriptables/Shop/ShopContent.asset");
            SkinItem pacman = AssetDatabase.LoadAssetAtPath<SkinItem>("Assets/MadSlime/Scriptables/Skins/Pacman.asset");
            RouletteTestStorage storage = new RouletteTestStorage();
            SkinInventory inventory = new SkinInventory(storage);

            for (int i = 0; i < content.SkinItems.Count; i++)
            {
                SkinItem skin = content.SkinItems[i];

                if (skin.Id != pacman.Id)
                {
                    inventory.Grant(skin.Id);
                }
            }

            SkinShowcase showcase = new SkinShowcase(_config, storage, inventory, content.SkinItems);
            showcase.Refresh();
            Require(showcase.HasAvailableSkins() == true, "Locked Common skin disappeared after higher rarities were exhausted.");
            List<RouletteSlot> slots = showcase.CreateSlots(8);
            float totalWeight = 0f;

            for (int i = 0; i < slots.Count; i++)
            {
                Require(slots[i].Skin == pacman, "Common fallback included an already owned skin.");
                totalWeight += slots[i].Weight;
            }

            Require(Mathf.Approximately(totalWeight, new RouletteProbabilityRules(_config).GetSkinDropChance(SkinRarity.Common)),
                "Common fallback changed the configured rarity weight.");
            inventory.Grant(pacman.Id);
            showcase.Refresh();
            Require(showcase.HasAvailableSkins() == false, "Exhausted catalog still permits paid spins.");
        }

        private void Commit(Action operation)
        {
            operation();
            Require(_inventory.IsOpen(_rewardSkin.Id) == true, "Save happened before the reward was granted.");
            _commits++;
        }

        private List<RouletteSlot> CreateSlots()
        {
            List<RouletteSlot> slots = new List<RouletteSlot>();
            slots.Add(new RouletteSlot(_rewardSkin, 0, 1f, false));
            return slots;
        }

        private RoulettePrize ResolvePrize(RouletteSlot slot)
        {
            return new RoulettePrize(slot.Skin, 0);
        }

        private bool CanSpin(bool advertisementGranted)
        {
            if (_requiresAd == true)
            {
                return advertisementGranted;
            }

            return _wallet.Balance >= GetPrice();
        }

        private void Pay(bool advertisementGranted)
        {
            if (advertisementGranted == false)
            {
                _wallet.Spend(GetPrice());
            }
        }

        private int GetPrice()
        {
            return 100;
        }

        private int GetZero()
        {
            return 0;
        }

        private void Require(bool condition, string message)
        {
            if (condition == false)
            {
                throw new InvalidOperationException(message);
            }
        }
    }
}
