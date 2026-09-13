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
        public ItemTier Tier => Definition.Tier;
        public Transform Self => transform;

        private void Awake()
        {
            _defaultScale = transform.localScale;
        }

        public void SetDefinition(ItemDefinition definition)
        {
            if (definition == null)
            {
                throw new ArgumentNullException(nameof(definition),
                    $"{name}: SetDefinition requires a non-null definition.");
            }

            _definition = definition;
        }

        public void Initialize(Vector3 position, float scale)
        {
            transform.position = position;
            transform.localScale = _defaultScale * scale;
            _collider.enabled = true;
        }

        public void Collect()
        {
            _collider.enabled = false;
        }

        public void Shutdown()
        {
            gameObject.SetActive(false);
        }
    }
}
