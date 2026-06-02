using System;
using UnityEngine;

namespace Collectables
{
    public sealed class Item : MonoBehaviour, IAttractable, IMassHolder
    {
        private const float MinDistanceSqr = 0.0001f;
        private const int MinMass = 1;

        [SerializeField] private ItemDefinition _definition;
        [SerializeField] private Collider _collider;

        private Vector3 _defaultScale = Vector3.one;

        public ItemDefinition Definition => _definition;
        public int Mass => _definition.BaseMass;

        public event Action<Item> ReadyToRelease;
        public event Action Collected;

        public void Initialize(Vector3 position)
        {
            transform.position = position;
            transform.localScale = _defaultScale;
            _collider.enabled = true;
        }

        public void Collect()
        {
            _collider.enabled = false;
            Collected?.Invoke();
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

            int safeMass = Mathf.Max(Mass, MinMass);
            float speed = attractForce / safeMass;

            Vector3 direction = toTarget.normalized;
            transform.position += direction * (speed * Time.deltaTime);
        }
    }
}