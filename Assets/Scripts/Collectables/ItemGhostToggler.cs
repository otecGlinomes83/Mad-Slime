using System;
using System.Collections.Generic;
using Items;
using Player;
using UnityEngine;

namespace Collectables
{
    public sealed class ItemGhostToggler : MonoBehaviour
    {
        private const int BufferSize = 64;

        [SerializeField] private PlayerTier _tierHolder;
        [SerializeField] private CapsuleCollider _playerCollider;
        [SerializeField] private LayerMask _layerMask;
        [SerializeField] private float _margin = 0.2f;

        private readonly Collider[] _buffer = new Collider[BufferSize];
        private readonly List<Item> _ghostItems = new List<Item>();

        private void Awake()
        {
            if (_tierHolder == null)
            {
                throw new InvalidOperationException(
                    $"{name}: TierHolder is not assigned. Drag a PlayerTier component into the _tierHolder field.");
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

        private void OnEnable()
        {
            float capsuleWorldRadius = _playerCollider.radius * Mathf.Abs(_playerCollider.transform.lossyScale.x);

            Debug.Log(
                $"[Diag] {name}: capsule localRadius={_playerCollider.radius} worldRadius={capsuleWorldRadius} margin={_margin}");
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

                Debug.Log($"[Diag] {name}: ghost OFF '{ghostItem.name}' #{ghostItem.GetInstanceID()}");
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

                if (item.Tier <= _tierHolder.CurrentTier)
                {
                    continue;
                }

                if (_ghostItems.Contains(item))
                {
                    continue;
                }

                item.SetGhost(true);
                _ghostItems.Add(item);

                float distance = Vector3.Distance(transform.position, item.transform.position);

                Debug.Log(
                    $"[Diag] {name}: ghost ON '{item.name}' #{item.GetInstanceID()} itemTier={item.Tier} " +
                    $"playerTier={_tierHolder.CurrentTier} distance={distance:0.0} " +
                    $"itemPos={item.transform.position} itemScale={item.transform.lossyScale} timeScale={Time.timeScale}");
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
