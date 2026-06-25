using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Game
{
    public sealed class Timer : MonoBehaviour
    {
        private float _duration;
        private float _remaining;
        private bool _isSetupFinished;
        private bool _isRunning = false;

        private CancellationTokenSource _runCancellationTokenSource;

        public event Action Finished;
        public event Action<float> Ticked;

        public void Setup(float duration)
        {
            if (duration <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(duration),
                    "Timer.Setup requires a positive duration in seconds.");
            }

            _duration = duration;
            _remaining = duration;
            _isSetupFinished = true;
        }

        public void StartCount()
        {
            if (_isSetupFinished == false)
            {
                throw new InvalidOperationException("Timer.Start called before Setup. Call Setup(duration) first.");
            }

            if (_isRunning == true)
            {
                throw new InvalidOperationException("Timer is already running.");
            }

            StartInternal();
        }

        public void Stop()
        {
            if (_isRunning == false)
            {
                return;
            }

            _runCancellationTokenSource?.Cancel();
            _runCancellationTokenSource?.Dispose();
            _runCancellationTokenSource = null;
            _isRunning = false;
        }

        public void Continue()
        {
            if (_isSetupFinished == false)
            {
                throw new InvalidOperationException("Timer.Continue called before Setup. Call Setup(duration) first.");
            }

            if (_isRunning == true)
            {
                throw new InvalidOperationException("Timer is already running.");
            }

            if (_remaining <= 0f)
            {
                throw new InvalidOperationException(
                    "Timer cannot be continued because remaining time is zero. Call Setup to reset duration.");
            }

            StartInternal();
        }

        private void StartInternal()
        {
            CancellationToken destroyToken = this.GetCancellationTokenOnDestroy();
            _runCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(destroyToken);
            CancellationToken linkedToken = _runCancellationTokenSource.Token;

            _isRunning = true;

            RunTimerAsync(linkedToken).Forget();
        }

        private async UniTaskVoid RunTimerAsync(CancellationToken cancellationToken)
        {
            try
            {
                while (_remaining > 0f)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);

                    if (Time.timeScale > 0f)
                    {
                        float delta = Time.deltaTime;
                        _remaining -= delta;

                        Ticked?.Invoke(_remaining);
                    }
                }

                _remaining = 0f;
                _isRunning = false;

                Finished?.Invoke();
            }
            catch (OperationCanceledException)
            {
                return;
            }
            finally
            {
                _runCancellationTokenSource?.Dispose();
                _runCancellationTokenSource = null;
            }
        }
    }
}
