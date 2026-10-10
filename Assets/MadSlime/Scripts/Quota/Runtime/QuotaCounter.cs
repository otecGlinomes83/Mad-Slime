using System;
using Collectables;
using Items;
using UnityEngine;
using Upgrades;

namespace Quota
{
    public class QuotaCounter
    {
        private const float MaxFillPercent = 1f;
        private const float QuotaFillWeight = 1f;

        private QuotaBoard _board;
        private PlayerUpgrades _upgrades;

        private ItemCollector _attachedCollector;
        private int _collectedQuotaCount;
        private float _collectedForeignFill;
        private bool _isCompleted;

        public event Action QuotaCompleted;

        public int CollectedQuotaCount => _collectedQuotaCount;

        public int TotalQuotaTarget => _board.TotalTarget;

        public float GetFillPercent()
        {
            if (_board.TotalTarget <= 0)
            {
                return 0f;
            }

            float percent = (_collectedQuotaCount * QuotaFillWeight + _collectedForeignFill) / _board.TotalTarget;
            return Mathf.Clamp01(percent);
        }

        public QuotaCounter(QuotaBoard board, PlayerUpgrades upgrades)
        {
            if (board == null)
            {
                throw new ArgumentNullException(nameof(board),
                    "QuotaCounter requires a QuotaBoard.");
            }

            if (upgrades == null)
            {
                throw new ArgumentNullException(nameof(upgrades),
                    "QuotaCounter requires PlayerUpgrades.");
            }

            _board = board;
            _upgrades = upgrades;
        }

        public void Attach(ItemCollector collector)
        {
            if (collector == null)
            {
                throw new ArgumentNullException(nameof(collector),
                    "QuotaCounter.Attach requires an ItemCollector.");
            }

            if (_attachedCollector != null)
            {
                throw new InvalidOperationException(
                    "QuotaCounter.Attach: the counter is already attached to a collector. Detach first.");
            }

            _attachedCollector = collector;
            _attachedCollector.ItemCollected += OnItemCollected;
        }

        public void Detach()
        {
            if (_attachedCollector == null)
            {
                return;
            }

            _attachedCollector.ItemCollected -= OnItemCollected;
            _attachedCollector = null;
        }

        public void ResetSession()
        {
            _collectedQuotaCount = 0;
            _collectedForeignFill = 0f;
            _isCompleted = false;
        }

        private void OnItemCollected(Item item)
        {
            if (_board.TryRegisterCollected(item.Definition) == true)
            {
                _collectedQuotaCount++;
                CheckCompletion();
                return;
            }

            _collectedForeignFill += _upgrades.ForeignFillMultiplier;
        }

        private void CheckCompletion()
        {
            if (_isCompleted == true)
            {
                return;
            }

            if (_collectedQuotaCount < _board.TotalTarget)
            {
                return;
            }

            _isCompleted = true;
            QuotaCompleted?.Invoke();
        }
    }
}
