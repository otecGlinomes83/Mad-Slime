using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

public sealed class PlayerScaler : MonoBehaviour
{
    [SerializeField] private PlayerMass _playerMass;
    [SerializeField] private Transform _transfromToScale;
    [SerializeField] private ItemDetector _itemDetector;
    [SerializeField] private AttractableDetector _attractableDetector;
    [SerializeField] private CollectableAttractor _collectableAttractor;
    [SerializeField] private float _scaleDivisor = 20f;
    [SerializeField] private float _attractionForceDivisor = 80f;
    [SerializeField] private float _smoothTime = 0.4f;

    private float _startScale;
    private float _itemDetectorStartRadius;
    private float _attractableDetectorStartRadius;
    private float _attractionStartForce;

    private int _startMass;

    private float _currentMultiplier;
    private float _targetMultiplier;
    private float _multiplierVelocity;

    private float _currentAttractionMultiplier;
    private float _targetAttractionMultiplier;
    private float _attractionVelocity;

    private void Awake()
    {
        if (_playerMass == null)
        {
            throw new InvalidOperationException(
                "PlayerScaler requires _playerMass to be assigned. The player mass is null.");
        }

        if (_transfromToScale == null)
        {
            throw new InvalidOperationException(
                "PlayerScaler requires _transfromToScale to be assigned. The transform to scale is null.");
        }

        if (_itemDetector == null)
        {
            throw new InvalidOperationException(
                "PlayerScaler requires _itemDetector to be assigned. The item detector is null.");
        }

        if (_attractableDetector == null)
        {
            throw new InvalidOperationException(
                "PlayerScaler requires _attractableDetector to be assigned. The attractable detector is null.");
        }

        if (_collectableAttractor == null)
        {
            throw new InvalidOperationException(
                "PlayerScaler requires _collectableAttractor to be assigned. The collectable attractor is null.");
        }

        if (_attractionForceDivisor <= 0f)
        {
            throw new InvalidOperationException(
                "PlayerScaler requires _attractionForceDivisor to be positive. The value is non-positive.");
        }

        _startScale = _transfromToScale.localScale.x;
        _itemDetectorStartRadius = _itemDetector.Radius;
        _attractableDetectorStartRadius = _attractableDetector.Radius;
        _attractionStartForce = _collectableAttractor.Force;
        _startMass = _playerMass.Mass;

        _currentMultiplier = 1f;
        _targetMultiplier = 1f;
        _multiplierVelocity = 0f;

        _currentAttractionMultiplier = 1f;
        _targetAttractionMultiplier = 1f;
        _attractionVelocity = 0f;

        ApplyMultiplier();
    }

    private void OnEnable()
    {
        _playerMass.Changed += OnMassChanged;
        ScalingLoop(this.GetCancellationTokenOnDestroy()).Forget();
    }

    private void OnDisable()
    {
        _playerMass.Changed -= OnMassChanged;
    }

    private void OnMassChanged(int previous, int current)
    {
        _targetMultiplier = 1f + (current - _startMass) / _scaleDivisor;
        _targetAttractionMultiplier = 1f + (current - _startMass) / _attractionForceDivisor;

        if (current < previous)
        {
            _currentMultiplier = _targetMultiplier;
            _currentAttractionMultiplier = _targetAttractionMultiplier;
            _multiplierVelocity = 0f;
            _attractionVelocity = 0f;
            ApplyMultiplier();
        }
    }

    private void ApplyMultiplier()
    {
        _transfromToScale.localScale = Vector3.one * (_startScale * _currentMultiplier);
        _itemDetector.SetRadius(_itemDetectorStartRadius * _currentMultiplier);
        _attractableDetector.SetRadius(_attractableDetectorStartRadius * _currentMultiplier);
        _collectableAttractor.SetForce(_attractionStartForce * _currentAttractionMultiplier);
    }

    private async UniTaskVoid ScalingLoop(CancellationToken cancellationToken)
    {
        try
        {
            while (cancellationToken.IsCancellationRequested == false)
            {
                if (Mathf.Approximately(_currentMultiplier, _targetMultiplier) == false)
                {
                    _currentMultiplier = Mathf.SmoothDamp(
                        _currentMultiplier,
                        _targetMultiplier,
                        ref _multiplierVelocity,
                        _smoothTime);

                    ApplyMultiplier();
                }

                if (Mathf.Approximately(_currentAttractionMultiplier, _targetAttractionMultiplier) == false)
                {
                    _currentAttractionMultiplier = Mathf.SmoothDamp(
                        _currentAttractionMultiplier,
                        _targetAttractionMultiplier,
                        ref _attractionVelocity,
                        _smoothTime);

                    ApplyMultiplier();
                }

                await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            return;
        }
    }
}
