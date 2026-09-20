using Game;
using Scriptables;
using System;
using UnityEngine;
using VContainer;

namespace Audio
{
    public sealed class TimerTickSound : MonoBehaviour
    {
        [SerializeField] private Timer _timer;
        [SerializeField] private SfxClip _sfxClip;
        [SerializeField] private float _thresholdSeconds = 20f;

        private SfxPlayer _sfxPlayer;
        private bool _isTickingActive;

        [Inject]
        public void Construct(SfxPlayer sfxPlayer)
        {
            _sfxPlayer = sfxPlayer;
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
                    $"{name}: Timer is not assigned.");
            }

            if (_sfxClip == null)
            {
                throw new InvalidOperationException(
                    $"{name}: SfxClip is not assigned. Drag a SfxClip asset into the _sfxClip field.");
            }
        }

        private void OnEnable()
        {
            _timer.Ticked += OnTimerTicked;
            _timer.Finished += OnTimerFinished;
        }

        private void OnDisable()
        {
            _timer.Ticked -= OnTimerTicked;
            _timer.Finished -= OnTimerFinished;

            if (_isTickingActive == true)
            {
                _sfxPlayer.StopLoop();
                _isTickingActive = false;
            }
        }

        private void OnTimerTicked(float remaining)
        {
            if (remaining > 0f && remaining <= _thresholdSeconds)
            {
                if (_isTickingActive == false)
                {
                    _sfxPlayer.StartLoop(_sfxClip);
                    _isTickingActive = true;
                }
            }
            else
            {
                if (_isTickingActive == true)
                {
                    _sfxPlayer.StopLoop();
                    _isTickingActive = false;
                }
            }
        }

        private void OnTimerFinished()
        {
            if (_isTickingActive == true)
            {
                _sfxPlayer.StopLoop();
                _isTickingActive = false;
            }
        }
    }
}