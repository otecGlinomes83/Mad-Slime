using System;
using Player;
using Skills;
using UnityEngine;
using VContainer;

namespace Detection
{
    public abstract class GenericOverlapDetector<T> : MonoBehaviour where T : class
    {
        private const int BufferSize = 256;

        [SerializeField] private float _radius = 1.5f;
        [SerializeField] private LayerMask _layerMask;
        [SerializeField] private Color _gizmoColor = Color.cyan;

        private readonly Collider[] _buffer = new Collider[BufferSize];

        private PlayerTier _tierSource;
        private TierResolver _tierResolver;
        private float _baseRadius;

        public event Action<T> Detected;

        public float Radius => _radius;

        [Inject]
        public void Construct(PlayerTier tierSource, TierResolver tierResolver)
        {
            _tierSource = tierSource;
            _tierResolver = tierResolver;
        }

        private void Awake()
        {
            if (_tierSource == null)
            {
                throw new InvalidOperationException(
                    $"{name}: TierSource was not injected. Check that GameLifetimeScope registers PlayerTier and the detector.");
            }

            if (_tierResolver == null)
            {
                throw new InvalidOperationException(
                    $"{name}: TierResolver was not injected. Check that GameLifetimeScope registers TierResolver and the detector.");
            }

            _baseRadius = _radius;
        }

        protected virtual void Update()
        {
            if (Time.timeScale == 0f)
            {
                return;
            }

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
            _tierSource.TierChanged += OnTierSourceChanged;
            SetRadius(_baseRadius * _tierResolver.GetScaleFor(_tierSource.CurrentTier));
        }

        protected virtual void OnDisable()
        {
            _tierSource.TierChanged -= OnTierSourceChanged;
        }

        public void SetRadius(float newRadius)
        {
            if (newRadius < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(newRadius), "new radius cannot be negative");
            }

            _radius = newRadius;
        }

        private void OnTierSourceChanged(ItemTier previousTier, ItemTier currentTier)
        {
            float newRadius = _baseRadius * _tierResolver.GetScaleFor(currentTier);

            SetRadius(newRadius);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = _gizmoColor;
            Gizmos.DrawWireSphere(transform.position, _radius);
        }
    }
}