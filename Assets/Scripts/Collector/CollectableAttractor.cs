using UnityEngine;

public sealed class CollectableAttractor : MonoBehaviour
{
    private const int BufferSize = 32;

    [SerializeField] private float _attractionRadius = 5f;
    [SerializeField] private float _attractionForce = 12f;
    [SerializeField] private LayerMask _attractableMask;
    [SerializeField] private Player _player;

    private readonly Collider[] _attractionBuffer = new Collider[BufferSize];

    public float Radius => _attractionRadius;

    private void Update()
    {
        int hitsCount =
            Physics.OverlapSphereNonAlloc(transform.position, _attractionRadius, _attractionBuffer, _attractableMask);

        for (int i = 0; i < hitsCount; i++)
        {
            Collider hitCollider = _attractionBuffer[i];

            if (hitCollider.TryGetComponent(out IAttractable attractable) == false)
            {
                continue;
            }

            if (attractable.Mass > _player.Mass)
            {
                return;
            }

            attractable.Attract(transform.position, _attractionForce);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, _attractionRadius);
    }
}