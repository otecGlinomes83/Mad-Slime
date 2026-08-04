using System;
using System.Collections.Generic;
using UnityEngine;
using Items;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UI
{
    public class ItemSpawner : MonoBehaviour
    {
        [SerializeField] private Item _prefab;

        private List<Item> _items = new List<Item>();

        private void OnDestroy()
        {
            for (int i = _items.Count-1; i >= 0; i--)
            {
                Destroy(_items[i].gameObject);
            }

            _items.Clear();
        }

        public Item Spawn()
        {
            Item item = Instantiate(_prefab);
            
#if UNITY_EDITOR
            Undo.RegisterCreatedObjectUndo(item.gameObject, "Spawn Item");
#endif
            _items.Add(item);

            return item;
        }
    }
}