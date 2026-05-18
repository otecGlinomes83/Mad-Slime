using UnityEngine;

public sealed class CameraFollow : MonoBehaviour
{
    [SerializeField] private Transform _target;
    [SerializeField] private Vector3 _offset = new Vector3(0f, 10f, -10f);
    [SerializeField] private float _smoothTime = 0.25f;
    [SerializeField] private float _maxFollowSpeed = 50f;

    private Vector3 _currentVelocity;

    private void LateUpdate()
    {
        if (_target == null)
        {
            return;
        }

        Vector3 desiredPosition = _target.position + _offset;

        transform.position = Vector3.SmoothDamp(
            transform.position,
            desiredPosition,
            ref _currentVelocity,
            _smoothTime,
            _maxFollowSpeed);
    }
}
