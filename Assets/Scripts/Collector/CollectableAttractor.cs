using System;
using UnityEngine;

public sealed class CollectableAttractor : MonoBehaviour
{
    private const float MinDistanceSqr = 0.0001f;

    [SerializeField] private float _attractionForce = 12f;
    [SerializeField] private AttractableDetector _detector;
    [SerializeField] private MonoBehaviour _massHolderSource;

    private IMassHolder _massHolder;

    public float Radius => _detector.Radius;
    public float Force => _attractionForce;

    public void SetForce(float force)
    {
        _attractionForce = force;
    }

    private void Awake()
    {
        if (_massHolderSource.TryGetComponent(out IMassHolder massHolder))
        {
            _massHolder = massHolder;
        }
        else
        {
            throw new InvalidOperationException(
                $"SessionHandler: {_massHolderSource.name} does not implement IMassHolder.");
        }
    }

    private void OnEnable()
    {
        _detector.Detected += OnDetected;
    }

    private void OnDisable()
    {
        _detector.Detected -= OnDetected;
    }

    private void OnDetected(IAttractable attractable)
    {
        if (attractable.Mass > _massHolder.Mass)
        {
            return;
        }

        Transform target = attractable.Self;
        Vector3 toTarget = transform.position - target.position;
        toTarget.y = 0f;

        if (toTarget.sqrMagnitude < MinDistanceSqr)
        {
            return;
        }

        float speed = _attractionForce / attractable.Mass;

        target.position += toTarget.normalized * (speed * Time.deltaTime);
    }
}
