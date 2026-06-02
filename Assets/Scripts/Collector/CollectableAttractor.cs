using System;
using UnityEngine;

public sealed class CollectableAttractor : MonoBehaviour
{
    [SerializeField] private float _attractionForce = 12f;
    [SerializeField] private AttractableDetector _detector;
    [SerializeField] private MonoBehaviour _massHolderSource;

    private IMassHolder _massHolder;

    public float Radius => _detector.Radius;

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

        attractable.Attract(transform.position, _attractionForce);
    }
}