using UnityEngine;

public sealed class CollectableAttractor : MonoBehaviour
{
    private const int BufferSize = 32;

    [SerializeField] private float _attractionRadius = 5f;
    [SerializeField] private float _strength = 50f;
    [SerializeField] private LayerMask _collectableMask;

    private readonly Collider[] _attractionBuffer = new Collider[BufferSize];
    private int _minMass=1;

    public float Radius => _attractionRadius;

    private void FixedUpdate()
    {
        int hitsCount = Physics.OverlapSphereNonAlloc(transform.position, _attractionRadius, _attractionBuffer, _collectableMask);

        for (int i = 0; i < hitsCount; i++)
        {
            Collider hitCollider = _attractionBuffer[i];

            if (hitCollider.TryGetComponent(out ICollectable collectable) == false)
            {
                continue;
            }

            if (hitCollider.TryGetComponent(out Rigidbody collectableRigidbody) == false)
            {
                continue;
            }

            ApplyAttraction(collectableRigidbody, collectable);
        }
    }

    private void ApplyAttraction(Rigidbody collectableRigidbody, ICollectable collectable)
    {
        Vector3 toPlayer = transform.position - collectableRigidbody.position;
        Vector3 direction = toPlayer.normalized;

        int mass = Mathf.Max(collectable.Mass, _minMass);
        float pullForce = _strength / mass;

        collectableRigidbody.AddForce(direction * pullForce, ForceMode.Acceleration);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, _attractionRadius);
    }
}
