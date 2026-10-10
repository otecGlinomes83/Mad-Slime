using CameraSystem;
using System;
using System.Collections.Generic;
using Skills;
using UnityEngine;
using VContainer;

namespace Player
{
    public class PlayerScaler : MonoBehaviour
    {
        [SerializeField] private Transform _modelTransform;
        [SerializeField] private Transform _rootTransform;
        [SerializeField] private CapsuleCollider _playerCollider;
        [SerializeField] private CameraFollow _cameraFollow;
        [SerializeField] private GrowthAnimator _growthAnimator;
        [SerializeField] private List<RadiusRecipientEntry> _radiusRecipients = new List<RadiusRecipientEntry>();

        private PlayerTier _playerTier;
        private TierResolver _tierResolver;

        private float _baseColliderRadius;
        private float _baseColliderHeight;
        private float _baseColliderCenterY;
        private float _currentMultiplier = 1f;

        [Inject]
        public void Construct(PlayerTier playerTier, TierResolver tierResolver)
        {
            _playerTier = playerTier;
            _tierResolver = tierResolver;
        }

        private void Awake()
        {
            if (_playerTier == null)
            {
                throw new InvalidOperationException(
                    $"{name}: PlayerTier was not injected. Check that GameLifetimeScope registers PlayerTier and PlayerScaler.");
            }

            if (_modelTransform == null)
            {
                throw new InvalidOperationException($"{name}: ModelTransform is not assigned.");
            }

            if (_rootTransform == null)
            {
                throw new InvalidOperationException($"{name}: RootTransform is not assigned.");
            }

            if (_playerCollider == null)
            {
                throw new InvalidOperationException($"{name}: PlayerCollider is not assigned.");
            }

            if (_cameraFollow == null)
            {
                throw new InvalidOperationException(
                    $"{name}: CameraFollow is not assigned. Drag the camera's CameraFollow component into the _cameraFollow field.");
            }

            if (_growthAnimator == null)
            {
                throw new InvalidOperationException(
                    $"{name}: GrowthAnimator is not assigned. Drag the GrowthAnimator component into the _growthAnimator field.");
            }

            for (int i = 0; i < _radiusRecipients.Count; i++)
            {
                _radiusRecipients[i].Initialize(name);
            }

            _baseColliderHeight = _playerCollider.height;
            _baseColliderRadius = _playerCollider.radius;
            _baseColliderCenterY = _playerCollider.center.y;
        }

        private void OnEnable()
        {
            _playerTier.TierChanged += OnTierChanged;

        }

        private void OnDisable()
        {
            _playerTier.TierChanged -= OnTierChanged;
        }

        public void ApplyInitialScale()
        {
            SizeTier currentTier = _playerTier.CurrentTier;

            _cameraFollow.SetOffsetMultiplier(_tierResolver.GetCameraOffsetFor(currentTier));

            float targetMultiplier = _tierResolver.GetScaleFor(currentTier);

            _currentMultiplier = targetMultiplier;
            ApplyMultiplier();
        }

        private void OnTierChanged(SizeTier previousTier, SizeTier currentTier)
        {
            _cameraFollow.SetOffsetMultiplier(_tierResolver.GetCameraOffsetFor(currentTier));

            float targetMultiplier = _tierResolver.GetScaleFor(currentTier);

            _growthAnimator.Play(_currentMultiplier, targetMultiplier, OnGrowthValueChanged);
        }

        private void OnGrowthValueChanged(float multiplier)
        {
            _currentMultiplier = multiplier;

            ApplyMultiplier();
        }

        private void ApplyMultiplier()
        {
            _modelTransform.localScale = Vector3.one * _currentMultiplier;
            _modelTransform.localPosition = new Vector3(0f, _baseColliderCenterY * _currentMultiplier, 0f);

            _playerCollider.height = _baseColliderHeight * _currentMultiplier;
            _playerCollider.radius = _baseColliderRadius * _currentMultiplier;
            _playerCollider.center = new Vector3(0f, _baseColliderCenterY * _currentMultiplier, 0f);

            _rootTransform.position =
                new Vector3(_rootTransform.position.x, _playerCollider.radius, _rootTransform.position.z);

            for (int i = 0; i < _radiusRecipients.Count; i++)
            {
                RadiusRecipientEntry entry = _radiusRecipients[i];

                entry.Recipient.SetRadius(entry.BaseRadius * _currentMultiplier);
            }
        }
    }
}
