using System;
using UnityEngine;

namespace Collectables
{
    [RequireComponent(typeof(Collider))]
    [RequireComponent(typeof(Rigidbody))]
    public sealed class SimpleCollectable : MonoBehaviour, ICollectable
    {
        [SerializeField] private int _mass = 1;

        private Collider _collider;
        private Rigidbody _rigidbody;

        public int Mass => _mass;

        public event Action<ICollectable> ReadyToRelease;

        private void Awake()
        {
            _collider = GetComponent<Collider>();
            _rigidbody = GetComponent<Rigidbody>();
        }

        public void Collect()
        {
            _collider.enabled = false;
            _rigidbody.isKinematic = true;
        }

        public void Release()
        {
            ReadyToRelease?.Invoke(this);
        }
    }
}