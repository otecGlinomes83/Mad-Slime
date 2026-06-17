using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class PlayerScaler : MonoBehaviour
{
    [SerializeField] private PlayerMass _playerMass;
    [SerializeField] private Transform _transfromToScale;
    [SerializeField] private ItemDetector _itemDetector;
    [SerializeField] private AttractableDetector _attractableDetector;
    [SerializeField] private float _scaleDivisor = 20f;
    [SerializeField] private float _smoothTime = 0.4f;

    private float _startScale;
    private float _itemDetectorStartRadius;
    private float _attractableDetectorStartRadius;

    private int _startMass;

    private float _currentMultiplier;
    private float _targetMultiplier;
    private float _multiplierVelocity;

    private bool _isChanging;

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

        _startScale = _transfromToScale.localScale.x;
        _itemDetectorStartRadius = _itemDetector.Radius;
        _attractableDetectorStartRadius = _attractableDetector.Radius;
        _startMass = _playerMass.Mass;

        _currentMultiplier = 1f;
        _targetMultiplier = 1f;
        _multiplierVelocity = 0f;

        ApplyMultiplier();
    }

    private void OnEnable()
    {
        _playerMass.Changed += OnMassChanged;
    }

    private void OnDisable()
    {
        _playerMass.Changed -= OnMassChanged;
    }

    private void OnMassChanged(int previous, int current)
    {
        _targetMultiplier = 1f + (current - _startMass) / _scaleDivisor;
        ChangeScale().Forget();
    }

    private void ApplyMultiplier()
    {
        _transfromToScale.localScale = Vector3.one * (_startScale * _currentMultiplier);
        _itemDetector.SetRadius(_itemDetectorStartRadius * _currentMultiplier);
        _attractableDetector.SetRadius(_attractableDetectorStartRadius * _currentMultiplier);
    }

    private async UniTaskVoid ChangeScale()
    {
        if (_isChanging)
        {
            return;
        }

        _isChanging = true;

        while (Mathf.Approximately(_currentMultiplier, _targetMultiplier) == false)
        {
            _currentMultiplier = Mathf.SmoothDamp(
                _currentMultiplier,
                _targetMultiplier,
                ref _multiplierVelocity,
                _smoothTime);

            ApplyMultiplier();
        }
        
        _isChanging = false;
    }
}