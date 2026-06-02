using System;
using UnityEngine;

namespace NPC.Prey
{
    public sealed class PlayerSensor : MonoBehaviour
    {
        private const int BufferSize = 4;
        private const float CheckInterval = 0.2f;

        [SerializeField] private float _radius = 5f;
        [SerializeField] private LayerMask _playerLayer;

        private readonly Collider[] _buffer = new Collider[BufferSize];
        
        private Transform _detectedPlayer;
        private bool _isPlayerInRange = false;

        public event Action<Transform> PlayerEntered;
        public event Action PlayerExited;

        public bool IsPlayerInRange => _isPlayerInRange;
        public Transform DetectedPlayer => _detectedPlayer;

        private void Update()
        {
            int hitsCount = Physics.OverlapSphereNonAlloc(
                transform.position,
                _radius,
                _buffer,
                _playerLayer);

            if (hitsCount > 0)
            {
                Transform player = _buffer[0].transform;

                if (_isPlayerInRange == false)
                {
                    _isPlayerInRange = true;
                    _detectedPlayer = player;

                    PlayerEntered?.Invoke(player);
                }
                else
                {
                    _detectedPlayer = player;
                }

                return;
            }

            if (_isPlayerInRange == true)
            {
                _isPlayerInRange = false;
                _detectedPlayer = null;

                PlayerExited?.Invoke();
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, _radius);
        }
    }
}