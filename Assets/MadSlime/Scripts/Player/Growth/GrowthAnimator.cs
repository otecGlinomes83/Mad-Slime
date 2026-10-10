using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using UnityEngine;

namespace Player
{
    public class GrowthAnimator : MonoBehaviour
    {
        [Tooltip("Длительность роста модели до нового тира (с).")]
        [SerializeField, Min(0.01f)] private float _growDuration = 0.55f;

        [Tooltip("Сила перелёта: 0 = без перелёта, 1.7 = классический овершут.")]
        [SerializeField, Min(0f)] private float _overshoot = 1.7f;

        private CancellationTokenSource _growCancellationTokenSource;

        public void Play(float fromValue, float targetValue, Action<float> onValueChanged)
        {
            if (onValueChanged == null)
            {
                throw new ArgumentNullException(nameof(onValueChanged),
                    "GrowthAnimator.Play requires a value listener.");
            }

            CancelGrowth();

            _growCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(
                this.GetCancellationTokenOnDestroy());

            GrowAsync(fromValue, targetValue, onValueChanged, _growCancellationTokenSource.Token).Forget();
        }

        private void OnDisable()
        {
            CancelGrowth();
        }

        private async UniTaskVoid GrowAsync(float fromValue, float targetValue, Action<float> onValueChanged,
            CancellationToken cancellationToken)
        {
            float elapsedTime = 0f;

            try
            {
                while (elapsedTime < _growDuration)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    elapsedTime += Time.deltaTime;
                    float progress = Mathf.Clamp01(elapsedTime / _growDuration);

                    onValueChanged.Invoke(
                        Mathf.LerpUnclamped(fromValue, targetValue, GetBackOutProgress(progress)));

                    await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
                }
            }
            catch (OperationCanceledException)
            {
                return;
            }

            onValueChanged.Invoke(targetValue);
        }

        private float GetBackOutProgress(float progress)
        {
            float shiftedProgress = progress - 1f;

            return 1f
                + (_overshoot + 1f) * shiftedProgress * shiftedProgress * shiftedProgress
                + _overshoot * shiftedProgress * shiftedProgress;
        }

        public void Stop()
        {
            CancelGrowth();
        }

        private void CancelGrowth()
        {
            if (_growCancellationTokenSource == null)
            {
                return;
            }

            _growCancellationTokenSource.Cancel();
            _growCancellationTokenSource.Dispose();
            _growCancellationTokenSource = null;
        }
    }
}
