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
        public Transform Self => transform;

        public int Mass
        {
            get
            {
                if (_definition == null)
                {
                    throw new InvalidOperationException(
                        $"{name}: Mass requested but Definition is null. Assign a definition on the prefab or via SetDefinition.");
                }

                return _definition.BaseMass;
            }
        }

        public ItemTier Tier
        {
            get
            {
                if (_definition == null)
                {
                    throw new InvalidOperationException(
                        $"{name}: Tier requested but Definition is null. Assign a definition on the prefab or via SetDefinition.");
                }

                return _definition.Tier;
            }
        }

        private void Awake()
        {
            if (_collider == null)
            {
                throw new InvalidOperationException(
                    $"{name}: Collider is not assigned. Drag a Collider into the _collider field.");
            }

            _defaultScale = new Vector3(
                Mathf.Abs(transform.localScale.x),
                Mathf.Abs(transform.localScale.y),
                Mathf.Abs(transform.localScale.z));
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
