using Player;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Detection
{
    public abstract class GenericOverlapDetector<T> : MonoBehaviour, IRadiusRecipient where T : class
    {
        private const int BufferSize = 256;

        [SerializeField] private float _radius = 1.5f;
        [SerializeField] private LayerMask _layerMask;
        [SerializeField] private Color _gizmoColor = Color.cyan;

        private Collider[] _buffer = new Collider[BufferSize];
        private HashSet<T> _previousTargets = new HashSet<T>();
        private HashSet<T> _currentTargets = new HashSet<T>();

        public event Action<T> Detected;

        public event Action<T> Exited;

        public float Radius => _radius;

        protected virtual void Update()
        {
            _currentTargets.Clear();
            int hitsCount = Physics.OverlapSphereNonAlloc(transform.position, _radius, _buffer, _layerMask);

            for (int i = 0; i < hitsCount; i++)
            {
                if (_buffer[i].TryGetComponent(out T target) == false || _currentTargets.Add(target) == false)
                {
                    continue;
                }

                Detected?.Invoke(target);
            }

            foreach (T target in _previousTargets)
            {
                if (_currentTargets.Contains(target) == false)
                {
                    Exited?.Invoke(target);
                }
            }

            HashSet<T> reusableTargets = _previousTargets;
            _previousTargets = _currentTargets;
            _currentTargets = reusableTargets;
        }

        protected virtual void OnDisable()
        {
            foreach (T target in _previousTargets)
            {
                Exited?.Invoke(target);
            }

            _previousTargets.Clear();
            _currentTargets.Clear();
        }

        public void SetRadius(float radius)
        {
            if (radius < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(radius));
            }

            _radius = radius;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = _gizmoColor;
            Gizmos.DrawWireSphere(transform.position, _radius);
        }
    }
}
