using System.Collections.Generic;
using Items;
using UnityEngine;

namespace Game
{
    public sealed class ItemPool
    {
        private readonly Dictionary<Item, List<Item>> _pooledByPrefab = new Dictionary<Item, List<Item>>();
        private readonly Dictionary<Item, Item> _prefabByInstance = new Dictionary<Item, Item>();
        private readonly Transform _root;

        public ItemPool()
        {
            GameObject rootObject = new GameObject("PooledItems");
            _root = rootObject.transform;
        }

        public Item Get(Item prefab)
        {
            if (prefab.gameObject.activeSelf == false)
            {
                Debug.LogWarning(
                    $"ItemPool: prefab '{prefab.name}' is disabled on the asset. Instantiated items will be invisible.");
            }

            if (_pooledByPrefab.TryGetValue(prefab, out List<Item> pooled) && pooled.Count > 0)
            {
                Item item = pooled[pooled.Count - 1];
                pooled.RemoveAt(pooled.Count - 1);

                item.gameObject.SetActive(true);
                return item;
            }

            Item created = Object.Instantiate(prefab, _root);
            _prefabByInstance[created] = prefab;

            return created;
        }

        public void Release(Item item)
        {
            item.Shutdown();

            if (_prefabByInstance.TryGetValue(item, out Item prefab) == false)
            {
                Object.Destroy(item.gameObject);
                return;
            }

            if (_pooledByPrefab.TryGetValue(prefab, out List<Item> pooled) == false)
            {
                pooled = new List<Item>();
                _pooledByPrefab[prefab] = pooled;
            }

            item.transform.SetParent(_root, false);
            pooled.Add(item);
        }
    }
}
