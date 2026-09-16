using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Player
{
    public sealed class ScalePunch : MonoBehaviour
    {
        [SerializeField] private Transform _target;
        [SerializeField, Range(0f, 0.5f)] private float _strength = 0.12f;
        [SerializeField, Min(0.01f)] private float _duration = 0.2f;
        [SerializeField, Range(0f, 0.5f)] private float _maxStackedStrength = 0.3f;

        private Vector3 _baseScale;
        private float _currentStrength;
        private bool _isPunching;
        private CancellationTokenSource _punchCts;

        private void Awake()
        {
            if (_target == null)
            {
                throw new InvalidOperationException(
                    $"{name}: Target is not assigned. Drag a Transform into the _target field.");
            }

            _baseScale = _target.localScale;
        }

        private void OnDisable()
        {
            CancelPunch();

            _currentStrength = 0f;

            if (_target != null)
            {
                _target.localScale = _baseScale;
            }
        }

        public void Punch(float strengthMultiplier)
        {
            if (strengthMultiplier < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(strengthMultiplier),
                    "ScalePunch.Punch requires a non-negative strength multiplier.");
            }

            _currentStrength = Mathf.Min(_currentStrength + _strength * strengthMultiplier, _maxStackedStrength);

            if (_isPunching == true)
            {
                return;
            }

            _isPunching = true;

            if (_punchCts != null)
            {
                _punchCts.Dispose();
            }

            _punchCts = CancellationTokenSource.CreateLinkedTokenSource(this.GetCancellationTokenOnDestroy());
            PunchAsync(_punchCts.Token).Forget();
        }

        private async UniTaskVoid PunchAsync(CancellationToken cancellationToken)
        {
            float elapsedTime = 0f;

            try
            {
                while (elapsedTime < _duration)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    elapsedTime += Time.deltaTime;
                    float progress = Mathf.Clamp01(elapsedTime / _duration);
                    float punch = Mathf.Sin(progress * Mathf.PI) * _currentStrength;

                    _target.localScale = _baseScale * (1f + punch);

                    await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
                }
            }
            catch (OperationCanceledException)
            {
                _isPunching = false;
                return;
            }

            _currentStrength = 0f;
            _isPunching = false;
            _target.localScale = _baseScale;
        }

        private void CancelPunch()
        {
            if (_punchCts == null)
            {
                return;
            }

            _punchCts.Cancel();
            _punchCts.Dispose();
            _punchCts = null;
            _isPunching = false;
        }
    }
}
