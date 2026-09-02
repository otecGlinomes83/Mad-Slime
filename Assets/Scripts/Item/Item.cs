using System;
using Interfaces;
using Skills;
using UnityEngine;

namespace Items
{
    public sealed class Item : MonoBehaviour, IAttractable
    {
        [SerializeField] private ItemDefinition _definition;
        [SerializeField] private Collider _collider;

        private Vector3 _defaultScale;

        public ItemDefinition Definition => _definition;
        public int Mass => _definition.BaseMass;
        public ItemTier Tier =>Definition.Tier;
        public Transform Self => transform;

        public event Action Collected;

        private void Awake()
        {
            _defaultScale = transform.localScale;
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
            Collected?.Invoke();
        }

        public void Shutdown()
        {
            gameObject.SetActive(false);
        }
    }
}
