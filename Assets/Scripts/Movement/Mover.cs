using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public sealed class Mover : MonoBehaviour
{
    [SerializeField] private float _maxSpeed = 6f;
    [SerializeField] private float _acceleration = 30f;
    [SerializeField] private float _deceleration = 30f;

    private CharacterController _characterController;
    private Vector3 _currentVelocity;

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
        float rate;

        if (targetVelocity.sqrMagnitude > _currentVelocity.sqrMagnitude)
        {
            rate = _acceleration;
        }
        else
        {
            rate = _deceleration;
        }

        _currentVelocity = Vector3.MoveTowards(_currentVelocity, targetVelocity, rate * Time.deltaTime);

        _characterController.SimpleMove(_currentVelocity);
    }
}
