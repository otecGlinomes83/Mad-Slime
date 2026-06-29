using System;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public sealed class Mover : MonoBehaviour
{
    [SerializeField] private float _defaultSpeed = 4f;
    [SerializeField] private float _maxSpeed = 9f;
    [SerializeField] private float _smoothTime = 0.12f;
    [SerializeField] private Game.Timer _sprintTimer;
    [SerializeField] private float _sprintDuration = 10f;
    [SerializeField] private float _sprintCooldown = 5f;

    private CharacterController _characterController;
    private Vector3 _currentVelocity;
    private Vector3 _velocityRef;
    private float _currentSpeed;

    private bool _isSprintActive;
    private bool _isOnCooldown;

    public Vector3 Velocity => _currentVelocity;
    public float ActualSpeed => _currentVelocity.magnitude;
    public bool IsSprintActive => _isSprintActive;

    public event Action SprintStarted;
    public event Action SprintEnded;
    public event Action CooldownEnded;

    private void Awake()
    {
        _characterController = GetComponent<CharacterController>();
        _currentSpeed = _defaultSpeed;

        if (_sprintTimer == null)
        {
            throw new InvalidOperationException(
                $"{name}: Sprint Timer is not assigned. Drag a Timer component into the _sprintTimer field in the inspector.");
        }
    }

    private void OnEnable()
    {
        _sprintTimer.Finished += OnSprintTimerFinished;
    }

    private void OnDisable()
    {
        _sprintTimer.Finished -= OnSprintTimerFinished;
    }

    public bool TryStartSprint()
    {
        if (_isSprintActive == true || _isOnCooldown == true)
        {
            return false;
        }

        _currentSpeed = _maxSpeed;
        _isSprintActive = true;
        _sprintTimer.Setup(_sprintDuration);
        _sprintTimer.StartCount();
        SprintStarted?.Invoke();
        return true;
    }

    public void DisableSprint()
    {
        if (_isSprintActive == false && _isOnCooldown == false)
        {
            return;
        }

        _sprintTimer.Stop();
        _isSprintActive = false;
        _isOnCooldown = false;
        _currentSpeed = _defaultSpeed;
        SprintEnded?.Invoke();
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

    private void OnSprintTimerFinished()
    {
        if (_isSprintActive == true)
        {
            _isSprintActive = false;
            _currentSpeed = _defaultSpeed;
            _isOnCooldown = true;
            _sprintTimer.Setup(_sprintCooldown);
            _sprintTimer.StartCount();
            SprintEnded?.Invoke();
        }
        else if (_isOnCooldown == true)
        {
            _isOnCooldown = false;
            CooldownEnded?.Invoke();
        }
    }
}