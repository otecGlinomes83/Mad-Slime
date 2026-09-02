using System;
using Player;
using Skills;
using UnityEngine;

namespace Detection
{
    public abstract class GenericOverlapDetector<T> : MonoBehaviour where T : class
    {
        private const int BufferSize = 256;

        [SerializeField] private float _radius = 1.5f;
        [SerializeField] private LayerMask _layerMask;
        [SerializeField] private Color _gizmoColor = Color.cyan;
        [SerializeField] private PlayerTier _tierSource;
        [SerializeField] private TierResolver _tierResolver;

        private readonly Collider[] _buffer = new Collider[BufferSize];

        private float _baseRadius;

        public event Action<T> Detected;

        public float Radius => _radius;

        protected virtual void Update()
        {
            int hitsCount = Physics.OverlapSphereNonAlloc(transform.position, _radius, _buffer, _layerMask);

            for (int i = 0; i < hitsCount; i++)
            {
                if (_buffer[i].TryGetComponent(out T target) == false)
                {
                    continue;
                }

                Detected?.Invoke(target);
            }
        }

        protected virtual void OnEnable()
        {
            _baseRadius = _radius;

            if (_tierSource == null || _tierResolver == null)
            {
                return;
            }

            _tierSource.TierChanged += OnTierSourceChanged;
            SetRadius(_baseRadius * _tierResolver.GetScaleFor(_tierSource.CurrentTier));
        }

        protected virtual void OnDisable()
        {
            if (_tierSource == null || _tierResolver == null)
            {
                return;
            }

            _tierSource.TierChanged -= OnTierSourceChanged;
        }

        public void SetRadius(float newRadius)
        {
            if (newRadius < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(newRadius), $"new radius cannot be negative");
            }

            _radius = newRadius;
        }

        private void OnTierSourceChanged(ItemTier previousTier, ItemTier currentTier)
        {
            SetRadius(_baseRadius * _tierResolver.GetScaleFor(currentTier));
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = _gizmoColor;
            Gizmos.DrawWireSphere(transform.position, _radius);
        }
    }
}
