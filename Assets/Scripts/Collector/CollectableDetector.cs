using System;
using Collectables;
using UnityEngine;

public sealed class CollectableDetector : MonoBehaviour
{
    private const int BufferSize = 16;

    [SerializeField] private float _detectionRadius = 1.5f;
    [SerializeField] private LayerMask _itemMask;

    private readonly Collider[] _detectionBuffer = new Collider[BufferSize];

    public event Action<Item> Detected;

    private void Update()
    {
        int hitsCount = Physics.OverlapSphereNonAlloc(transform.position, _detectionRadius, _detectionBuffer, _itemMask);

        for (int i = 0; i < hitsCount; i++)
        {
            Collider hitCollider = _detectionBuffer[i];

            if (hitCollider.TryGetComponent(out Item item) == false)
            {
                continue;
            }

            Detected?.Invoke(item);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, _detectionRadius);
    }
}
