using System;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Items
{
    [RequireComponent(typeof(ItemVisual))]
    public class ItemBody : MonoBehaviour
    {
        [SerializeField] private Collider _collider;

        private Vector3 _defaultScale;
        private ItemVisual _visual;

        public Collider Collider => _collider;

        private void Awake()
        {
            if (_collider == null || TryGetComponent(out _visual) == false)
            {
                throw new InvalidOperationException($"{name}: collider and ItemVisual are required.");
            }

            _defaultScale = transform.localScale;
        }

        public void Initialize(Vector3 position, float scale)
        {
            transform.position = position;
            transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
            transform.localScale = _defaultScale * scale;
            _collider.enabled = true;
            _visual.ResetVisuals();
        }

        public void Capture()
        {
            _collider.enabled = false;
            _visual.ResetVisuals();
        }
    }
}
