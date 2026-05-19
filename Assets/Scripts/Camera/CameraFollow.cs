using System;
using UnityEngine;

public sealed class CameraFollow : MonoBehaviour
{
    [SerializeField] private Transform _target;
    [SerializeField] private float _smoothTime = 0.25f;
    [SerializeField] private float _maxFollowSpeed = 50f;

    private Vector3 _currentVelocity;

    private Vector3 _offset;

    private void Awake()
    {
        _offset = transform.position - _target.position;
    }

    private void LateUpdate()
    {
        Vector3 desiredPosition = _target.position + _offset;

        transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref _currentVelocity, _smoothTime,
            _maxFollowSpeed);
    }
}