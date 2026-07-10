using Collectables;
using Cysharp.Threading.Tasks;
using Player;
using System;
using System.Threading;
using UnityEngine;

namespace Skills
{
    public sealed class LevelScaler : MonoBehaviour
    {
        [SerializeField] private PlayerTier _playerTier;
        [SerializeField] private Transform _modelTransform;
        [SerializeField] private CharacterController _characterController;
        [SerializeField] private ItemDetector _itemDetector;
        [SerializeField] private AttractableDetector _attractableDetector;
        [SerializeField] private TierResolver _tierResolver;
        [SerializeField] private float _smoothTime = 0.4f;

        private float _baseControllerHeight;
        private float _baseControllerRadius;
        private float _baseControllerCenterY;
        private float _itemDetectorStartRadius;
        private float _attractableDetectorStartRadius;

        private float _currentMultiplier = 1f;
        private float _targetMultiplier = 1f;
        private float _multiplierVelocity;

        private float _lastAppliedMultiplier = -1f;

        private CancellationTokenSource _growCts;

        private ItemTier _currentTier = ItemTier.Small;

        private void Awake()
        {
            if (_playerTier == null)
            {
                throw new InvalidOperationException("LevelScaler requires _playerTier to be assigned.");
            }

            if (_modelTransform == null)
            {
                throw new InvalidOperationException("LevelScaler requires _modelTransform to be assigned.");
            }

            if (_characterController == null)
            {
                throw new InvalidOperationException("LevelScaler requires _characterController to be assigned.");
            }

            if (_itemDetector == null)
            {
                throw new InvalidOperationException("LevelScaler requires _itemDetector to be assigned.");
            }

            if (_attractableDetector == null)
            {
                throw new InvalidOperationException("LevelScaler requires _attractableDetector to be assigned.");
            }

            if (_tierResolver == null)
            {
                throw new InvalidOperationException("LevelScaler requires _tierResolver to be assigned.");
            }

            _baseControllerHeight = _characterController.height;
            _baseControllerRadius = _characterController.radius;
            _baseControllerCenterY = _characterController.center.y;
            _itemDetectorStartRadius = _itemDetector.Radius;
            _attractableDetectorStartRadius = _attractableDetector.Radius;

            ApplyMultiplier();
        }

        private void OnEnable()
        {
            _playerTier.TierChanged += OnTierChanged;
        }

        private void OnDisable()
        {
            _playerTier.TierChanged -= OnTierChanged;

            if (_growCts != null)
            {
                _growCts.Cancel();
                _growCts.Dispose();
                _growCts = null;
            }
        }

        private void OnTierChanged(ItemTier previousTier, ItemTier currentTier)
        {
            if (currentTier == _currentTier)
            {
                return;
            }

            _currentTier = currentTier;
            _targetMultiplier = _tierResolver.GetScaleFor(_currentTier);

            GrowAsync().Forget();
        }

        private async UniTaskVoid GrowAsync()
        {
            if (_growCts != null)
            {
                _growCts.Cancel();
                _growCts.Dispose();
            }

            _growCts = CancellationTokenSource.CreateLinkedTokenSource(this.GetCancellationTokenOnDestroy());
            CancellationToken cancellationToken = _growCts.Token;

            try
            {
                while (Mathf.Abs(_currentMultiplier - _targetMultiplier) > 0.001f)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    _currentMultiplier = Mathf.SmoothDamp(
                        _currentMultiplier,
                        _targetMultiplier,
                        ref _multiplierVelocity,
                        _smoothTime);

                    ApplyMultiplier();

                    await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
                }

                _currentMultiplier = _targetMultiplier;
                ApplyMultiplier();
            }
            catch (OperationCanceledException)
            {
                return;
            }
        }

        private void ApplyMultiplier()
        {
            _modelTransform.localScale = Vector3.one * _currentMultiplier;
            _modelTransform.localPosition = new Vector3(0f, _baseControllerCenterY * _currentMultiplier, 0f);

            if (Mathf.Abs(_currentMultiplier - _lastAppliedMultiplier) < 0.01f)
            {
                return;
            }

            _lastAppliedMultiplier = _currentMultiplier;

            _characterController.height = _baseControllerHeight * _currentMultiplier;
            _characterController.radius = _baseControllerRadius * _currentMultiplier;
            _characterController.center = new Vector3(0f, _baseControllerCenterY * _currentMultiplier, 0f);

            _itemDetector.SetRadius(_itemDetectorStartRadius * _currentMultiplier);
            _attractableDetector.SetRadius(_attractableDetectorStartRadius * _currentMultiplier);
        }
    }
}