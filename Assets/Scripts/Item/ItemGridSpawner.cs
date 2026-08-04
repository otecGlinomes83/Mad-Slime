using System;
using System.ComponentModel;
using Items;
using UnityEngine;

namespace UI
{
    public class ItemGridSpawner : MonoBehaviour
    {
        [SerializeField] private ItemSpawner _itemSpawner;
        [SerializeField] private Vector2 _spawnZone;
        [SerializeField] private Transform _itemParent;
        [SerializeField] private float _spacing;

        [ContextMenu("Spawn")]
        private void SpawnGrid()
        {
            if (_itemSpawner == null)
            {
                throw new NullReferenceException("ItemSpawner is null");
            }

            if (_itemParent == null)
            {
                throw new NullReferenceException("itemParent is null");
            }

            float offsetX = (_spawnZone.x - 1) * _spacing * 0.5f;
            float offsetZ = (_spawnZone.y - 1) * _spacing * 0.5f;

            for (int x = 0; x < _spawnZone.x; x++)
            {
                for (int z = 0; z < _spawnZone.y; z++)
                {
                    Item item = _itemSpawner.Spawn();

                    Vector3 worldPosition =
                        transform.TransformPoint(new Vector3(x * _spacing - offsetX, 0f, z * _spacing - offsetZ));
                    item.transform.position = worldPosition;

                    item.transform.SetParent(_itemParent, true);
                }
            }
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.yellow;
            float offsetX = (_spawnZone.x - 1) * _spacing * 0.5f;
            float offsetZ = (_spawnZone.y - 1) * _spacing * 0.5f;

            for (int x = 0; x < _spawnZone.x; x++)
            {
                for (int z = 0; z < _spawnZone.y; z++)
                {
                    Vector3 worldPos = transform.TransformPoint(
                        new Vector3(x * _spacing - offsetX, 0f, z * _spacing - offsetZ));

                    Gizmos.DrawSphere(worldPos, 0.25f);
                }
            }
        }
    }
}