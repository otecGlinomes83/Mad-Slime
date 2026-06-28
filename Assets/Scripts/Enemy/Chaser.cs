using UnityEngine;

public class Chaser : MonoBehaviour
{
    [SerializeField] private Mover _mover;
    [SerializeField] private Rotator _rotator;

    public void Tick(Vector3 playerPosition)
    {
        Vector3 offset = playerPosition - transform.position;
        Vector3 direction = offset.normalized;

        _mover.Move(direction);
        _rotator.Rotate(direction);
    }
}