using UnityEngine;

public sealed class NPCMover : MonoBehaviour
{
    [SerializeField] private float _defaultSpeed = 4f;

    public float DefaultSpeed => _defaultSpeed;

    public void Move(Vector3 worldDirection)
    {
        worldDirection.y = 0f;

        Vector3 targetPosition = transform.position + worldDirection * _defaultSpeed;
        transform.position = Vector3.MoveTowards(transform.position, targetPosition, _defaultSpeed * Time.deltaTime);
    }
}