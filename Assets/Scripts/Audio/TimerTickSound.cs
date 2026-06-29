using System;
using Game;
using UnityEngine;
using UnityEngine.Audio;

namespace Audio
{
    [RequireComponent(typeof(AudioSource))]
    public sealed class TimerTickSound : MonoBehaviour
    {
        [SerializeField] private Timer _timer;
        [SerializeField] private AudioMixerGroup _group;
        [SerializeField] private AudioClip _clip;
        [SerializeField] private float _thresholdSeconds = 20f;

        private AudioSource _source;
        private bool _isTickingActive;

        private void Awake()
        {
            if (_timer == null)
            {
                throw new InvalidOperationException(
                    $"{name}: Timer is not assigned.");
            }

            if (_clip == null)
            {
                throw new InvalidOperationException(
                    $"{name}: AudioClip is not assigned.");
            }

            _source = GetComponent<AudioSource>();
            _source.outputAudioMixerGroup = _group;
            _source.playOnAwake = false;
            _source.loop = true;
            _source.clip = _clip;
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
        }

        private void OnTimerTicked(float remaining)
        {
            if (remaining > 0f && remaining <= _thresholdSeconds)
            {
                if (_isTickingActive == false)
                {
                    _source.Play();
                    _isTickingActive = true;
                }
            }
            else
            {
                if (_isTickingActive == true)
                {
                    _source.Stop();
                    _isTickingActive = false;
                }
            }
        }

        private void OnTimerFinished()
        {
            if (_isTickingActive == true)
            {
                _source.Stop();
                _isTickingActive = false;
            }
        }
    }
}