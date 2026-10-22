using Player;
using System;
using System.Collections.Generic;
using Unity.Profiling;
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

        private ProfilerMarker _scanMarker;
        private ProfilerMarker _collectHitsMarker;
        private ProfilerMarker _processTargetsMarker;
        private ProfilerMarker _processExitsMarker;

        public event Action<T> Detected;

        public event Action<T> Exited;

        public float Radius => _radius;

        protected virtual void Awake()
        {
            string detectorName = GetType().Name;
            _scanMarker = new ProfilerMarker($"{detectorName}.Scan");
            _collectHitsMarker = new ProfilerMarker($"{detectorName}.CollectHits");
            _processTargetsMarker = new ProfilerMarker($"{detectorName}.ProcessTargets");
            _processExitsMarker = new ProfilerMarker($"{detectorName}.ProcessExits");
        }

        protected virtual void Update()
        {
            using ProfilerMarker.AutoScope scanScope = _scanMarker.Auto();

            _currentTargets.Clear();
            int hitsCount = CollectHits();
            ProcessTargets(hitsCount);
            ProcessExits();
            SwapTargetSets();
        }

        private int CollectHits()
        {
            using ProfilerMarker.AutoScope collectHitsScope = _collectHitsMarker.Auto();

            int hitsCount = Physics.OverlapSphereNonAlloc(transform.position, _radius, _buffer, _layerMask);

            while (hitsCount == _buffer.Length)
            {
                Array.Resize(ref _buffer, _buffer.Length * 2);
                hitsCount = Physics.OverlapSphereNonAlloc(transform.position, _radius, _buffer, _layerMask);
            }

            return hitsCount;
        }

        private void ProcessTargets(int hitsCount)
        {
            using ProfilerMarker.AutoScope processTargetsScope = _processTargetsMarker.Auto();

            for (int i = 0; i < hitsCount; i++)
            {
                if (_buffer[i].TryGetComponent(out T target) == false || _currentTargets.Add(target) == false)
                {
                    continue;
                }

                Detected?.Invoke(target);
            }
        }

        private void ProcessExits()
        {
            using ProfilerMarker.AutoScope processExitsScope = _processExitsMarker.Auto();

            foreach (T target in _previousTargets)
            {
                if (_currentTargets.Contains(target) == false)
                {
                    Exited?.Invoke(target);
                }
            }
        }

        private void SwapTargetSets()
        {
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

        private void OnDrawGizmos()
        {
            Gizmos.color = _gizmoColor;
            Gizmos.DrawWireSphere(transform.position, _radius);
        }
    }
}
