using System;
using UnityEngine;

namespace Item
{
    public sealed class Item : MonoBehaviour, IAttractable
    {
        [SerializeField] private ItemDefinition _definition;
        [SerializeField] private Collider _collider;

        private Vector3 _defaultScale = Vector3.one;

        public ItemDefinition Definition => _definition;
        public int Mass => _definition.BaseMass;
        public Transform Self => transform;

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

        public void Shutdown()
        {
            gameObject.SetActive(false);
        }
    }
}
