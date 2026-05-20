using System;
using UnityEngine;

namespace Collectables
{
    [RequireComponent(typeof(Collider))]
    public sealed class SimpleCollectable : MonoBehaviour, ICollectable, IAttractable
    {
        private const float MinDistanceSqr = 0.0001f;
        private const int MinMass = 1;

        [SerializeField] private int _mass = 1;

        private Collider _collider;
        private Vector3 _defaultScale = Vector3.one;

        private float _attractionSpeed;
        
        public int Mass => _mass;

        public event Action<SimpleCollectable> ReadyToRelease;

        private void Awake()
        {
            _collider = GetComponent<Collider>();
        }

        public void Initialize(Vector3 position)
        {
            transform.position = position;
            transform.localScale = _defaultScale;
            _collider.enabled = true;
        }

        public void Collect()
        {
            _collider.enabled = false;
        }

        public void Release()
        {
            ReadyToRelease?.Invoke(this);
        }

        public void Attract(Vector3 targetPosition, float attractForce)
        {
            Vector3 toTarget = targetPosition - transform.position;
            toTarget.y = 0f;

            if (toTarget.sqrMagnitude < MinDistanceSqr)
            {
                return;
            }

            int safeMass = Mathf.Max(_mass, MinMass);
            float speed = attractForce / safeMass;

            Vector3 direction = toTarget.normalized;
            transform.position += direction * (speed * Time.deltaTime);
        }

    }
}