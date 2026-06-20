using UnityEngine;

public sealed class CameraFollow : MonoBehaviour
{
    [SerializeField] private Transform _target;
    [SerializeField] private float _positionSmoothTime = 0.25f;
    [SerializeField] private float _maxPositionSpeed = 50f;
    [SerializeField] private float _maxRotationSpeed = 360f;

    private Vector3 _positionVelocity;
    private Vector3 _offset;

    private void Awake()
    {
        if (_target == null)
        {
            throw new System.InvalidOperationException(
                $"{name}: Target is not assigned. Drag a Transform into the _target field in the inspector.");
        }

        _offset = transform.position - _target.position;
    }

    private void LateUpdate()
    {
        Vector3 desiredPosition = _target.position + _offset;
        transform.position = Vector3.SmoothDamp(
            transform.position,
            desiredPosition,
            ref _positionVelocity,
            _positionSmoothTime,
            _maxPositionSpeed);

        Vector3 lookDirection = _target.position - transform.position;

        Quaternion desiredRotation = Quaternion.LookRotation(lookDirection);
        Quaternion currentRotation = transform.rotation;

        transform.rotation = Quaternion.Slerp(currentRotation, desiredRotation, _maxRotationSpeed * Time.deltaTime);
    }

    public void SetTarget(Transform newTarget)
    {
        _target = newTarget;
        _offset = transform.position - _target.position;
    }
}
