using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Audio
{
    public sealed class SoundLimiter : MonoBehaviour
    {
        [SerializeField] private int _maxConcurrent = 6;

        private int _playingCount;

        public bool TryPlay(float durationSeconds)
        {
            if (durationSeconds <= 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(durationSeconds), "SoundLimiter.TryPlay requires a positive duration in seconds.");
            }

            if (_playingCount >= _maxConcurrent)
            {
                return false;
            }

            _playingCount++;
            ReleaseAsync(durationSeconds, this.GetCancellationTokenOnDestroy()).Forget();

            return true;
        }

        private async UniTaskVoid ReleaseAsync(float durationSeconds, CancellationToken cancellationToken)
        {
            int delayMilliseconds = Mathf.CeilToInt(durationSeconds * 1000f);

            try
            {
                await UniTask.Delay(delayMilliseconds, cancellationToken: cancellationToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            _playingCount = Mathf.Max(0, _playingCount - 1);
        }
    }
}
