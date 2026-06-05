using UnityEngine;

public sealed class Rotator : MonoBehaviour
{
    private const float MinDirectionSqrMagnitude = 0.0001f;

    [SerializeField] private float _rotationSpeed = 420f;

    public void Rotate(Vector3 direction)
    {
        direction.y = 0f;

        if (direction.sqrMagnitude < MinDirectionSqrMagnitude)
        {
            return;
        }

        Quaternion targetRotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, _rotationSpeed * Time.deltaTime);
    }
}