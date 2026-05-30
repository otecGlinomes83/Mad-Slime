using System;
using UnityEngine;

public abstract class GenericOverlapDetector<T> : MonoBehaviour where T : class
{
    private const int BufferSize = 32;

    [SerializeField] private float _radius = 1.5f;
    [SerializeField] private LayerMask _layerMask;
    [SerializeField] private Color _gizmoColor = Color.cyan;

    private readonly Collider[] _buffer = new Collider[BufferSize];

    public event Action<T> Detected;

    public float Radius => _radius;

    protected virtual void Update()
    {
        int hitsCount = Physics.OverlapSphereNonAlloc(transform.position, _radius, _buffer, _layerMask);

        for (int i = 0; i < hitsCount; i++)
        {
            if (_buffer[i].TryGetComponent(out T target) == false)
            {
                continue;
            }

            Detected?.Invoke(target);
        }
    }

    public void ChangeRadius(float newRadius)
    {
        if (newRadius < 0f)
        {
            throw new ArgumentOutOfRangeException(nameof(newRadius), $"new radius cannot be negative");
        }
        
        _radius = newRadius;
    }
    
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = _gizmoColor;
        Gizmos.DrawWireSphere(transform.position, _radius);
    }
}