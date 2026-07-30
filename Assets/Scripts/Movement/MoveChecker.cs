using Interfaces;
using Player;
using UnityEngine;

public class MoveChecker : MonoBehaviour
{
    [SerializeField] private LayerMask _layerMask;
    [SerializeField] private PlayerTier _playerTier;
    [SerializeField] private CapsuleCollider _playerCollider;

    private Vector3 _lastPosition;
    private Vector3 _lastVelocity;

    public bool IsAbleToMove(Vector3 currentPosition, Vector3 velocity)
    {
        _lastPosition = currentPosition;
        _lastVelocity = velocity;

        Vector3 direction = velocity.normalized;
        float distance = velocity.magnitude * Time.deltaTime;

        if (Physics.SphereCast(currentPosition, _playerCollider.radius, direction, out RaycastHit hitInfo, distance, _layerMask))
        {
            if (hitInfo.collider.gameObject.TryGetComponent(out IAttractable attractable))
            {
                if (attractable.Tier > _playerTier.CurrentTier)
                {
                    return false;
                }
            }
        }

        return true;
    }

    private void OnDrawGizmos()
    {
        Vector3 endPosition = _lastPosition + _lastVelocity;

        Gizmos.color = Color.red;
        Gizmos.DrawSphere(endPosition,  _playerCollider.radius);

        Gizmos.color = Color.green;
        Gizmos.DrawSphere(_lastPosition,  _playerCollider.radius);

        Gizmos.DrawLine(_lastPosition, endPosition);
    }
}