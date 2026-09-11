using Interfaces;
using Player;
using System;
using UnityEngine;

namespace Movement
{
    public sealed class MoveChecker : MonoBehaviour
    {
        [SerializeField] private LayerMask _layerMask;
        [SerializeField] private PlayerTier _playerTier;
        [SerializeField] private CapsuleCollider _playerCollider;

        private Vector3 _lastPosition;
        private Vector3 _lastVelocity;

        private void Awake()
        {
            if (_playerTier == null)
            {
                throw new InvalidOperationException(
                    $"{name}: PlayerTier is not assigned. Drag a PlayerTier component into the _playerTier field.");
            }

            if (_playerCollider == null)
            {
                throw new InvalidOperationException(
                    $"{name}: PlayerCollider is not assigned. Drag a CapsuleCollider into the _playerCollider field.");
            }
        }

        public bool IsAbleToMove(Vector3 currentPosition, Vector3 velocity)
        {
            _lastPosition = currentPosition;
            _lastVelocity = velocity;

            Vector3 direction = velocity.normalized;
            float distance = velocity.magnitude * Time.deltaTime;

            if (Physics.SphereCast(currentPosition, _playerCollider.radius, direction, out RaycastHit hitInfo, distance, _layerMask) == false)
            {
                return true;
            }

            if (hitInfo.collider.gameObject.TryGetComponent(out IAttractable attractable) == false)
            {
                return false;
            }

            return attractable.Tier <= _playerTier.CurrentTier;
        }

        private void OnDrawGizmosSelected()
        {
            Vector3 endPosition = _lastPosition + _lastVelocity;

            Gizmos.color = Color.red;
            Gizmos.DrawSphere(endPosition, _playerCollider.radius);

            Gizmos.color = Color.green;
            Gizmos.DrawSphere(_lastPosition, _playerCollider.radius);

            Gizmos.DrawLine(_lastPosition, endPosition);
        }
    }
}
