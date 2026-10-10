using System;
using System.Collections.Generic;
using Collectables;
using Items;
using UnityEngine;

namespace Game
{
    public class LevelItemsSpawner
    {
        private ItemFactory _itemFactory;
        private Transform _itemsRoot;
        private ItemCollector _collector;
        private List<Item> _activeItems = new List<Item>(256);
        private Dictionary<ItemDefinition, int> _spawnedCounts = new Dictionary<ItemDefinition, int>();

        public IReadOnlyList<Item> ActiveItems => _activeItems;
        public Dictionary<ItemDefinition, int> SpawnedCounts => _spawnedCounts;

        public LevelItemsSpawner(ItemFactory itemFactory, Transform itemsRoot)
        {
            if (itemFactory == null)
            {
                throw new ArgumentNullException(nameof(itemFactory),
                    "LevelItemsSpawner requires an ItemFactory.");
            }

            if (itemsRoot == null)
            {
                throw new ArgumentNullException(nameof(itemsRoot),
                    "LevelItemsSpawner requires an items root transform.");
            }

            _itemFactory = itemFactory;
            _itemsRoot = itemsRoot;
        }

        public static float GetRadiusXZ(Item prefab)
        {
            if (prefab == null)
            {
                throw new ArgumentNullException(nameof(prefab),
                    "LevelItemsSpawner: Item prefab is not assigned.");
            }

            if (prefab.TryGetComponent(out ItemBody body) == false
                || body.Collider is BoxCollider boxCollider == false)
            {
                throw new InvalidOperationException(
                    $"{prefab.name}: Item prefab needs a BoxCollider in the Collider field on ItemBody to measure the XZ radius.");
            }

            Vector3 scaledSize = Vector3.Scale(boxCollider.size, prefab.transform.lossyScale);

            return Mathf.Max(scaledSize.x, scaledSize.z) * 0.5f;
        }

        public void Attach(ItemCollector collector)
        {
            if (collector == null)
            {
                throw new ArgumentNullException(nameof(collector));
            }

            Detach();
            _collector = collector;
            _collector.ItemCollected += OnItemCollected;
        }

        public void Detach()
        {
            if (_collector == null)
            {
                return;
            }

            _collector.ItemCollected -= OnItemCollected;
            _collector = null;
        }

        public void Clear()
        {
            foreach (Item item in _activeItems)
            {
                if (item != null)
                {
                    item.gameObject.SetActive(false);
                    UnityEngine.Object.Destroy(item.gameObject);
                }
            }

            _activeItems.Clear();
            _spawnedCounts.Clear();
        }

        public void Spawn(IReadOnlyList<LevelPlacement> placements)
        {
            for (int i = 0; i < placements.Count; i++)
            {
                LevelPlacement placement = placements[i];
                Item item = _itemFactory.Create(placement.Prefab, placement.Definition,
                    placement.Position, placement.Scale, _itemsRoot);
                _activeItems.Add(item);
                CountSpawned(placement.Definition);
            }
        }

        private void OnItemCollected(Item item)
        {
            _activeItems.Remove(item);
        }

        private void CountSpawned(ItemDefinition definition)
        {
            if (_spawnedCounts.TryGetValue(definition, out int count) == true)
            {
                _spawnedCounts[definition] = count + 1;
                return;
            }

            _spawnedCounts[definition] = 1;
        }
    }
}
