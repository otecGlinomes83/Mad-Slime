using Scriptables;
using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace Skills
{
    public sealed class LevelScaler : MonoBehaviour
    {
         [SerializeField] private PlayerTier _playerTier;
        [SerializeField] private Transform _transfromToScale;
        [SerializeField] private ItemDetector _itemDetector;
        [SerializeField] private AttractableDetector _attractableDetector;
        [SerializeField] private TierScalerConfig _tierConfig;
        [SerializeField] private float _smoothTime = 0.4f;

        private float _startScale;
        private float _itemDetectorStartRadius;
        private float _attractableDetectorStartRadius;

        private float _currentMultiplier;
        private float _targetMultiplier;
        private float _multiplierVelocity;

        private ItemTier _currentTier = ItemTier.Small;

        private void Awake()
        {
            if (_playerTier == null)
            {
                throw new InvalidOperationException(
                    "LevelScaler requires _playerMass to be assigned. The player mass is null.");
            }

            if (_transfromToScale == null)
            {
                throw new InvalidOperationException(
                    "LevelScaler requires _transfromToScale to be assigned. The transform to scale is null.");
            }

            if (_itemDetector == null)
            {
                throw new InvalidOperationException(
                    "LevelScaler requires _itemDetector to be assigned. The item detector is null.");
            }

            if (_attractableDetector == null)
            {
                throw new InvalidOperationException(
                    "LevelScaler requires _attractableDetector to be assigned. The attractable detector is null.");
            }

            if (_tierConfig == null)
            {
                throw new InvalidOperationException(
                    "LevelScaler requires _tierConfig to be assigned. The tier scaler config is null.");
            }

            _startScale = _transfromToScale.localScale.x;
            _itemDetectorStartRadius = _itemDetector.Radius;
            _attractableDetectorStartRadius = _attractableDetector.Radius;

            _currentMultiplier = 1f;
            _targetMultiplier = 1f;
            _multiplierVelocity = 0f;

            ApplyMultiplier();
        }

        private void OnEnable()
        {
            _playerTier.Changed += OnTierChanged;
        }

        private void OnDisable()
        {
            _playerTier.Changed -= OnTierChanged;
        }

        private void OnTierChanged(int previous, int current)
        {
            ItemTier unlockedTier = _tierConfig.GetUnlockedTier(current);

            if (unlockedTier == _currentTier)
            {
                return;
            }

            _currentTier = unlockedTier;
            _targetMultiplier = _tierConfig.GetScaleFor(_currentTier);
        }

        private void Update()
        {
            if (Mathf.Approximately(_currentMultiplier, _targetMultiplier))
            {
                return;
            }

            _currentMultiplier = Mathf.SmoothDamp(
                _currentMultiplier,
                _targetMultiplier,
                ref _multiplierVelocity,
                _smoothTime);

            ApplyMultiplier();
        }

        private void ApplyMultiplier()
        {
            _transfromToScale.localScale = Vector3.one * (_startScale * _currentMultiplier);
            _itemDetector.SetRadius(_itemDetectorStartRadius * _currentMultiplier);
            _attractableDetector.SetRadius(_attractableDetectorStartRadius * _currentMultiplier);
        }
    }
}