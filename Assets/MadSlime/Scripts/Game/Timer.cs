using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Game
{
    public sealed class Timer : MonoBehaviour
    {
        [Tooltip("Сколько последних секунд уровня считается финальным отсчётом: на нём тикает звук и включается Адреналин.")]
        [SerializeField, Min(0.1f)] private float _finalCountdownSeconds = 20f;

        private float _duration;
        private float _remaining;
        private bool _isSetupFinished;
        private bool _isRunning = false;
        private bool _isFinalCountdownStarted;

        private CancellationTokenSource _runCancellationTokenSource;

        public event Action Finished;
        public event Action Stopped;
        public event Action<float> Ticked;
        public event Action FinalCountdownStarted;

        public float Duration => _duration;

        public void Setup(float duration)
        {
            if (duration <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(duration),
                    "Timer.Setup requires a positive duration in seconds.");
            }

            _duration = duration;
            _remaining = duration;
            _isFinalCountdownStarted = false;
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

            Stopped?.Invoke();
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

                        if (_isFinalCountdownStarted == false && _remaining <= _finalCountdownSeconds)
                        {
                            _isFinalCountdownStarted = true;

                            FinalCountdownStarted?.Invoke();
                        }
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
