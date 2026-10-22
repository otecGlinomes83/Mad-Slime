using CameraSystem;
using Items;
using Player;
using System;
using System.Collections.Generic;
using UnityEngine;
using VContainer;

namespace Collectables
{
    [DefaultExecutionOrder(100)]
    public class CameraObstacleDetector : MonoBehaviour, IRadiusRecipient
    {
        private const int BufferSize = 256;

        [SerializeField, Min(0.001f)] private float _radius = 0.4f;
        [SerializeField] private LayerMask _layerMask;
        [SerializeField] private Color _gizmoColor = Color.cyan;

        private CameraFollow _cameraFollow;
        private PlayerScaler _playerScaler;
        private RaycastHit[] _buffer = new RaycastHit[BufferSize];
        private HashSet<GhostAnimator> _previousTargets = new HashSet<GhostAnimator>();
        private HashSet<GhostAnimator> _currentTargets = new HashSet<GhostAnimator>();

        public event Action<GhostAnimator> Detected;

        public event Action<GhostAnimator> Exited;

        public float Radius => _radius;

        [Inject]
        public void Construct(CameraFollow cameraFollow, PlayerScaler playerScaler)
        {
            _cameraFollow = cameraFollow;
            _playerScaler = playerScaler;
        }

        private void LateUpdate()
        {
            Vector3 origin = _cameraFollow.transform.position;
            Vector3 toPlayer = _playerScaler.WorldCenter - origin;
            float distance = toPlayer.magnitude;
            _currentTargets.Clear();

            if (distance > Mathf.Epsilon)
            {
                int hitsCount = CollectHits(origin, toPlayer / distance, distance);

                for (int i = 0; i < hitsCount; i++)
                {
                    GhostAnimator animator = _buffer[i].collider.GetComponentInParent<GhostAnimator>();

                    if (animator != null && animator.isActiveAndEnabled == true && _currentTargets.Add(animator) == true)
                    {
                        Detected?.Invoke(animator);
                    }
                }
            }

            foreach (GhostAnimator animator in _previousTargets)
            {
                if (animator != null && _currentTargets.Contains(animator) == false)
                {
                    Exited?.Invoke(animator);
                }
            }

            HashSet<GhostAnimator> reusableTargets = _previousTargets;
            _previousTargets = _currentTargets;
            _currentTargets = reusableTargets;
        }

        private int CollectHits(Vector3 origin, Vector3 direction, float distance)
        {
            int hitsCount = Physics.SphereCastNonAlloc(origin, _radius, direction, _buffer, distance, _layerMask);

            while (hitsCount == _buffer.Length)
            {
                Array.Resize(ref _buffer, _buffer.Length * 2);
                hitsCount = Physics.SphereCastNonAlloc(origin, _radius, direction, _buffer, distance, _layerMask);
            }

            return hitsCount;
        }

        private void OnDisable()
        {
            foreach (GhostAnimator animator in _previousTargets)
            {
                if (animator != null)
                {
                    Exited?.Invoke(animator);
                }
            }

            _previousTargets.Clear();
            _currentTargets.Clear();
        }

        public void SetRadius(float radius)
        {
            if (radius <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(radius));
            }

            _radius = radius;
        }

        private void OnDrawGizmosSelected()
        {
            if (_cameraFollow == null || _playerScaler == null)
            {
                return;
            }

            Vector3 origin = _cameraFollow.transform.position;
            Vector3 target = _playerScaler.WorldCenter;
            Gizmos.color = _gizmoColor;
            Gizmos.DrawLine(origin, target);
            Gizmos.DrawWireSphere(origin, _radius);
            Gizmos.DrawWireSphere(target, _radius);
        }
    }
}
