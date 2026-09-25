using System;
using System.Collections.Generic;
using Items;
using Player;
using UnityEngine;
using Upgrades;
using VContainer;

namespace Collectables
{
    public sealed class ItemGhostToggler : MonoBehaviour
    {
        private const int BufferSize = 64;

        [SerializeField] private CapsuleCollider _playerCollider;

        [SerializeField] private LayerMask _layerMask;

        [SerializeField] private float _margin = 0.2f;

        private readonly Collider[] _buffer = new Collider[BufferSize];
        private readonly List<Item> _ghostItems = new List<Item>();

        private PlayerTier _tierHolder;
        private PlayerUpgrades _upgrades;

        [Inject]
        public void Construct(PlayerTier tierHolder, PlayerUpgrades upgrades)
        {
            _tierHolder = tierHolder;
            _upgrades = upgrades;
        }

        private void Awake()
        {
            if (_tierHolder == null)
            {
                throw new InvalidOperationException(
                    $"{name}: TierHolder was not injected. Check that GameLifetimeScope registers PlayerTier and ItemGhostToggler.");
            }

            if (_playerCollider == null)
            {
                throw new InvalidOperationException(
                    $"{name}: PlayerCollider is not assigned. Drag a CapsuleCollider into the _playerCollider field.");
            }

            if (_margin < 0f)
            {
                throw new InvalidOperationException(
                    $"{name}: Margin cannot be negative. Set _margin to a non-negative value.");
            }
        }

        private void Update()
        {
            if (Time.timeScale == 0f)
            {
                return;
            }

            float capsuleWorldRadius = _playerCollider.radius * Mathf.Abs(_playerCollider.transform.lossyScale.x);
            float radius = capsuleWorldRadius + _margin;
            int hitsCount = Physics.OverlapSphereNonAlloc(transform.position, radius, _buffer, _layerMask);

            DisableGhosts(hitsCount);
            EnableGhosts(hitsCount);
        }

        private void DisableGhosts(int hitsCount)
        {
            for (int i = _ghostItems.Count - 1; i >= 0; i--)
            {
                Item ghostItem = _ghostItems[i];

                if (Contains(hitsCount, ghostItem))
                {
                    continue;
                }

                ghostItem.SetGhost(false);
                _ghostItems.RemoveAt(i);
            }
        }

        private void EnableGhosts(int hitsCount)
        {
            for (int i = 0; i < hitsCount; i++)
            {
                if (_buffer[i].TryGetComponent(out Item item) == false)
                {
                    continue;
                }

                if (item.Tier <= _tierHolder.CurrentTier + _upgrades.AmbitionTierOffset)
                {
                    continue;
                }

                if (_ghostItems.Contains(item))
                {
                    continue;
                }

                item.SetGhost(true);
                _ghostItems.Add(item);
            }
        }

        private bool Contains(int hitsCount, Item target)
        {
            for (int i = 0; i < hitsCount; i++)
            {
                if (_buffer[i].TryGetComponent(out Item item) && item == target)
                {
                    return true;
                }
            }

            return false;
        }
    }
}