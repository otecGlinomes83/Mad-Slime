using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public sealed class Mover : MonoBehaviour
{
    [SerializeField] private float _maxSpeed = 6f;
    [SerializeField] private float _acceleration = 30f;
    [SerializeField] private float _deceleration = 30f;

    private Rigidbody _rigidbody;
    private Vector3 _currentHorizontalVelocity;

    public Vector3 Velocity => _rigidbody.velocity;
    public float CurrentSpeed => _currentHorizontalVelocity.magnitude;

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
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

        if (targetVelocity.sqrMagnitude > _currentHorizontalVelocity.sqrMagnitude)
        {
            rate = _acceleration;
        }
        else
        {
            rate = _deceleration;
        }

        _currentHorizontalVelocity = Vector3.MoveTowards(_currentHorizontalVelocity, targetVelocity, rate * Time.fixedDeltaTime);

        Vector3 newVelocity = new Vector3(_currentHorizontalVelocity.x, _rigidbody.velocity.y, _currentHorizontalVelocity.z);
        _rigidbody.velocity = newVelocity;
    }
}
