using System;
using Items;
using UnityEngine;

namespace UI
{
    public class CircleItemSpawner : MonoBehaviour
    {
        [SerializeField] private ItemSpawner _itemSpawner;
        [SerializeField] private Transform _itemParent;
        [SerializeField] private int _count;
        [SerializeField] private float _radius;

        [ContextMenu("Spawn")]
        private void SpawnCircle()
        {
            float angleStep = 2f * Mathf.PI / _count;

            for (int i = 0; i < _count; i++)
            {
                float angle = angleStep * i;
                float x = Mathf.Cos(angle) * _radius;
                float z = Mathf.Sin(angle) * _radius;

                Item item = _itemSpawner.Spawn();
                item.transform.SetParent(_itemParent,true);

                item.transform.localPosition = new Vector3(x, 0f, z);
            }
        }

        private void OnDrawGizmos()
        {
            Gizmos.matrix = _itemParent.localToWorldMatrix;

            Gizmos.DrawWireSphere(Vector3.zero, _radius);
        }
    }
}