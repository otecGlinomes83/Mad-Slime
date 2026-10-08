using Game;
using Scriptables;
using System;
using UnityEngine;
using VContainer;

namespace Audio
{
    public sealed class TimerTickSound : MonoBehaviour
    {
        [SerializeField] private SfxClip _sfxClip;

        private SfxPlayer _sfxPlayer;
        private Timer _timer;
        private bool _isTickingActive;

        [Inject]
        public void Construct(SfxPlayer sfxPlayer, Timer timer)
        {
            _sfxPlayer = sfxPlayer;
            _timer = timer;
        }

        private void Awake()
        {
            if (_sfxPlayer == null)
            {
                throw new InvalidOperationException(
                    $"{name}: SfxPlayer was not injected. GameLifetimeScope must be the first object in the scene hierarchy.");
            }

            if (_timer == null)
            {
                throw new InvalidOperationException(
                    $"{name}: Timer was not injected. Check that GameLifetimeScope registers Timer and TimerTickSound.");
            }

            if (_sfxClip == null)
            {
                throw new InvalidOperationException(
                    $"{name}: SfxClip is not assigned. Drag a SfxClip asset into the _sfxClip field.");
            }
        }

        private void OnEnable()
        {
            _timer.FinalCountdownStarted += OnFinalCountdownStarted;
            _timer.Finished += OnTimerFinished;
            _timer.Stopped += OnTimerStopped;
        }

        private void OnDisable()
        {
            _timer.FinalCountdownStarted -= OnFinalCountdownStarted;
            _timer.Finished -= OnTimerFinished;
            _timer.Stopped -= OnTimerStopped;

            if (_isTickingActive == true)
            {
                _sfxPlayer.StopLoop();
                _isTickingActive = false;
            }
        }

        private void OnFinalCountdownStarted()
        {
            _sfxPlayer.StartLoop(_sfxClip);
            _isTickingActive = true;
        }

        private void OnTimerFinished()
        {
            StopTicking();
        }

        private void OnTimerStopped()
        {
            StopTicking();
        }

        private void StopTicking()
        {
            if (_isTickingActive == false)
            {
                return;
            }

            _sfxPlayer.StopLoop();
            _isTickingActive = false;
        }
    }
}
