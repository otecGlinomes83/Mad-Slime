using System;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public sealed class Mover : MonoBehaviour
{
    [SerializeField] private float _defaultSpeed = 4f;
    [SerializeField] private float _smoothTime = 0.12f;

    private CharacterController _characterController;
    private Vector3 _currentVelocity;
    private Vector3 _velocityRef;
    private float _currentSpeed;

    public Vector3 Velocity => _currentVelocity;
    public float ActualSpeed => _currentVelocity.magnitude;

    public event Action SpeedChanged;

    private void Awake()
    {
        _characterController = GetComponent<CharacterController>();
        _currentSpeed = _defaultSpeed;
    }

    public void SetSpeedMultiplier(float multiplier)
    {
        if (multiplier <= 0f)
        {
            return;
        }

        _currentSpeed = _defaultSpeed * multiplier;
        SpeedChanged?.Invoke();
    }

    public void ResetSpeed()
    {
        _currentSpeed = _defaultSpeed;
        SpeedChanged?.Invoke();
    }

    public void Move(Vector3 worldDirection)
    {
        Vector3 direction = worldDirection;

        if (direction.sqrMagnitude > 1f)
        {
            direction = direction.normalized;
        }

        Vector3 targetVelocity = direction * _currentSpeed;

        _currentVelocity = Vector3.SmoothDamp(
            _currentVelocity,
            targetVelocity,
            ref _velocityRef,
            _smoothTime
        );

        _characterController.SimpleMove(_currentVelocity);
    }
}