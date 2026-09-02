using System;
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
                return false;
            }

            if (_playingCount >= _maxConcurrent)
            {
                return false;
            }

            _playingCount++;
            ReleaseAsync(durationSeconds).Forget();

            return true;
        }

        private async UniTaskVoid ReleaseAsync(float durationSeconds)
        {
            int delayMilliseconds = Mathf.CeilToInt(durationSeconds * 1000f);

            await UniTask.Delay(delayMilliseconds);

            _playingCount = Mathf.Max(0, _playingCount - 1);
        }
    }
}
