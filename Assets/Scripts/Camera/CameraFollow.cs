using Player;
using Skills;
using System;
using UnityEngine;

namespace CameraSystem
{
    public sealed class CameraFollow : MonoBehaviour
    {
    [SerializeField] private Transform _target;
    [SerializeField] private TierResolver _tierResolver;
    [SerializeField] private PlayerTier _playerTier;
    [SerializeField] private float _positionSmoothTime = 0.25f;
    [SerializeField] private float _maxPositionSpeed = 50f;

    private Vector3 _positionVelocity;
    private Vector3 _currentOffset;

    private Vector3 _startOffset;

    private void Awake()
    {
        if (_target == null)
        {
            throw new InvalidOperationException(
                $"{name}: Target is not assigned. Drag a Transform into the _target field in the inspector.");
        }

        if (_tierResolver == null)
        {
            throw new InvalidOperationException(
                $"{name}: TierResolver is not assigned. Drag a TierResolver component into the _tierResolver field in the inspector.");
        }

        _currentOffset = transform.position - _target.position;
        _startOffset = _currentOffset;
    }

    private void OnEnable()
    {
        _playerTier.TierChanged += OnTierChanged;
    }

    private void OnDisable()
    {
        _playerTier.TierChanged -= OnTierChanged;
    }

    private void LateUpdate()
    {
        Vector3 desiredPosition = _target.position + _currentOffset;

        transform.position = Vector3.SmoothDamp(
            transform.position,
            desiredPosition,
            ref _positionVelocity,
            _positionSmoothTime,
            _maxPositionSpeed);
    }

    private void OnTierChanged(ItemTier previousTier, ItemTier currentTier)
    {
        float cameraOffsetMultiplier = _tierResolver.GetCameraOffsetFor(currentTier);

        _currentOffset = _startOffset * cameraOffsetMultiplier;
    }
    }
}