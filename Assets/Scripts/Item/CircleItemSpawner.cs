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
                item.transform.position = transform.TransformPoint(new Vector3(x, item.transform.position.y, z));
                item.transform.SetParent(_itemParent, true);
            }
        }

        private float GetGizmoRadius()
        {
            BoxCollider boxCollider = _itemSpawner.Prefab.GetComponent<BoxCollider>();
            Vector3 scale = _itemSpawner.Prefab.transform.localScale;

            Vector3 size = new Vector3(
                boxCollider.size.x * scale.x,
                boxCollider.size.y * scale.y,
                boxCollider.size.z * scale.z);

            return size.magnitude * 0.5f;
        }

        private void OnDrawGizmos()
        {
            float angleStep = 2f * Mathf.PI / _count;
            Gizmos.color = Color.yellow;
            
            for (int i = 0; i < _count; i++)
            {
                float angle = angleStep * i;
                float x = Mathf.Cos(angle) * _radius;
                float z = Mathf.Sin(angle) * _radius;

                Vector3 worldPoint = transform.TransformPoint(new Vector3(x, 0f, z));
                Gizmos.DrawSphere(worldPoint, GetGizmoRadius());
            }
        }
    }
}