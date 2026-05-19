using System;
using UnityEngine;

namespace Collectables
{
    [RequireComponent(typeof(Collider))]
    public sealed class SimpleCollectable : MonoBehaviour, ICollectable
    {
        [SerializeField] private int _mass = 1;

        private Collider _collider;

        public int Mass => _mass;

        public event Action<ICollectable> ReadyToRelease;

        private void Awake()
        {
            _collider = GetComponent<Collider>();
        }

        public void Collect()
        {
            _collider.enabled = false;
        }

        public void Release()
        {
            ReadyToRelease?.Invoke(this);
        }
    }
}