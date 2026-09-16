using Cysharp.Threading.Tasks;
using Movement;
using System;
using System.Threading;
using Skills;
using UnityEngine;

namespace Player
{
    public sealed class LevelScaler : MonoBehaviour
    {
        [SerializeField] private PlayerTier _playerTier;
        [SerializeField] private TierResolver _tierResolver;
        [Space]
        [SerializeField] private Transform _modelTransform;
        [SerializeField] private Transform _rootTransform;
        [Space]
        [SerializeField] private CapsuleCollider _playerCollider;
        [SerializeField] private Mover _mover;
        [Space]
        [SerializeField, Min(0.01f)] private float _growDuration = 0.55f;
        [SerializeField, Min(0f)] private float _overshoot = 1.7f;

        private float _baseControllerRadius;
        private float _baseControllerCenterY;
        private float _baseControllerHeight;

        private float _currentMultiplier = 1f;
        private float _targetMultiplier = 1f;

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

            if (_playerCollider == null)
            {
                throw new InvalidOperationException("LevelScaler requires _playerCollider to be assigned.");
            }

            if (_tierResolver == null)
            {
                throw new InvalidOperationException("LevelScaler requires _tierResolver to be assigned.");
            }

            if (_rootTransform == null)
            {
                throw new InvalidOperationException("LevelScaler requires _rootTransform to be assigned.");
            }

            if (_mover == null)
            {
                throw new InvalidOperationException("LevelScaler requires _mover to be assigned.");
            }

            if (_growDuration <= 0f)
            {
                throw new InvalidOperationException("LevelScaler requires a positive _growDuration.");
            }

            _baseControllerHeight = _playerCollider.height;
            _baseControllerRadius = _playerCollider.radius;
            _baseControllerCenterY = _playerCollider.center.y;

            ApplyMultiplier();
        }

        private void OnEnable()
        {
            _playerTier.TierChanged += OnTierChanged;

            _currentTier = _playerTier.CurrentTier;
            _targetMultiplier = _tierResolver.GetScaleFor(_currentTier);
            _currentMultiplier = _targetMultiplier;

            _mover.SetDefaultSpeed(_tierResolver.GetSpeedFor(_currentTier));
            ApplyMultiplier();
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
            _mover.SetDefaultSpeed(_tierResolver.GetSpeedFor(_currentTier));

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

            float startMultiplier = _currentMultiplier;
            float elapsedTime = 0f;

            try
            {
                while (elapsedTime < _growDuration)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    elapsedTime += Time.deltaTime;
                    float progress = Mathf.Clamp01(elapsedTime / _growDuration);

                    _currentMultiplier = Mathf.LerpUnclamped(startMultiplier, _targetMultiplier, GetBackOutProgress(progress));

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

        private float GetBackOutProgress(float progress)
        {
            float shiftedProgress = progress - 1f;

            return 1f
                + (_overshoot + 1f) * shiftedProgress * shiftedProgress * shiftedProgress
                + _overshoot * shiftedProgress * shiftedProgress;
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

            _playerCollider.height = _baseControllerHeight * _currentMultiplier;
            _playerCollider.radius = _baseControllerRadius * _currentMultiplier;
            _playerCollider.center = new Vector3(0f, _baseControllerCenterY * _currentMultiplier, 0f);

            _rootTransform.position =
                new Vector3(_rootTransform.position.x, _playerCollider.radius, _rootTransform.position.z);
        }
    }
}
