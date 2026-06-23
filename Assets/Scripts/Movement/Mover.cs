using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public sealed class Mover : MonoBehaviour
{
    [SerializeField] private float _maxSpeed = 6f;
    [SerializeField] private float _smoothTime = 0.12f;

    private CharacterController _characterController;
    private Vector3 _currentVelocity;
    private Vector3 _velocityRef;

    public Vector3 Velocity => _currentVelocity;
    public float CurrentSpeed => _currentVelocity.magnitude;

    private void Awake()
    {
        _characterController = GetComponent<CharacterController>();
    }

    public void Move(Vector3 worldDirection)
    {
        Vector3 direction = worldDirection;

        if (direction.sqrMagnitude > 1f)
        {
            direction = direction.normalized;
        }

        Vector3 targetVelocity = direction * _maxSpeed;

        _currentVelocity = Vector3.SmoothDamp(
            _currentVelocity,
            targetVelocity,
            ref _velocityRef,
            _smoothTime
        );

        _characterController.SimpleMove(_currentVelocity);
        
    }
}
