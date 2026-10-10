using Collectables;
using Items;
using Quota;
using Scriptables;
using System;
using UnityEngine;
using Upgrades;
using VContainer;

namespace Player
{
    public class MassGainer : MonoBehaviour
    {
        private const int MinMassPerItem = 1;

        private ItemCollector _itemCollector;
        private PlayerTier _playerTier;
        private PlayerUpgrades _upgrades;
        private QuotaBoard _quotaBoard;
        private TierLookup _tierLookup;
        private ScalePunch _collectPunch;

        [Inject]
        public void Construct(ItemCollector itemCollector, PlayerTier playerTier, PlayerUpgrades upgrades,
            QuotaBoard quotaBoard, TierTable tierTable, ScalePunch collectPunch)
        {
            _itemCollector = itemCollector;
            _playerTier = playerTier;
            _upgrades = upgrades;
            _quotaBoard = quotaBoard;
            _tierLookup = new TierLookup(tierTable);
            _collectPunch = collectPunch;
        }

        private void OnEnable()
        {
            _itemCollector.ItemCollected += OnItemCollected;
        }

        private void OnDisable()
        {
            _itemCollector.ItemCollected -= OnItemCollected;
        }

        private void OnItemCollected(Item item)
        {
            if (item.Definition.Tier == _playerTier.CurrentTier)
            {
                _collectPunch.Punch();
            }

            bool isQuotaItem = _quotaBoard.IsQuotaItem(item.Definition);

            float massMultiplier;

            if (isQuotaItem == true)
            {
                massMultiplier = _upgrades.GetQuotaMassMultiplier();
            }
            else
            {
                massMultiplier = _upgrades.GetMassMultiplier();
            }

            int mass = Mathf.Max(MinMassPerItem,
                Mathf.RoundToInt(_tierLookup.Get(item.Definition.Tier).Mass * massMultiplier));

            _playerTier.Add(mass);
        }
    }
}
