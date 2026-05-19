using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public sealed class Rotator : MonoBehaviour
{
    private const float MinDirectionSqrMagnitude = 0.0001f;

    [SerializeField] private float _rotationSpeed = 420f;

    private Rigidbody _rigidbody;

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
    }

    public void Rotate(Vector3 direction)
    {
        if (direction.sqrMagnitude < MinDirectionSqrMagnitude)
        {
            return;
        }

        Quaternion targetRotation = Quaternion.LookRotation(direction);
        Quaternion newRotation = Quaternion.RotateTowards(_rigidbody.rotation, targetRotation, _rotationSpeed * Time.fixedDeltaTime);

        _rigidbody.MoveRotation(newRotation);
    }
}