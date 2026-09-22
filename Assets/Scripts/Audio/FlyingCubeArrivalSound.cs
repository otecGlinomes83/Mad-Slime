using Scriptables;
using ShapeFill;
using System;
using UnityEngine;
using VContainer;

namespace Audio
{
    [RequireComponent(typeof(ShapeFiller))]
    public sealed class FlyingCubeArrivalSound : MonoBehaviour
    {
        [SerializeField] private SfxClip _sfxClip;
        [SerializeField, Range(0.5f, 2f)] private float _minPitch = 0.9f;
        [SerializeField, Range(0.5f, 2f)] private float _maxPitch = 1.35f;
        [SerializeField, Min(0f)] private float _minInterval = 0.03f;

        private SfxPlayer _sfxPlayer;
        private SoundLimiter _soundLimiter;
        private ShapeFiller _filler;
        private float _lastPlayedTime;

        [Inject]
        public void Construct(SfxPlayer sfxPlayer, SoundLimiter soundLimiter)
        {
            _sfxPlayer = sfxPlayer;
            _soundLimiter = soundLimiter;
        }

        private void Awake()
        {
            if (_sfxPlayer == null)
            {
                throw new InvalidOperationException(
                    $"{name}: SfxPlayer was not injected. FillLifetimeScope must be the first object in the scene hierarchy.");
            }

            if (_sfxClip == null)
            {
                throw new InvalidOperationException(
                    $"{name}: SfxClip is not assigned. Drag a SfxClip asset into the _sfxClip field.");
            }

            if (_soundLimiter == null)
            {
                throw new InvalidOperationException(
                    $"{name}: SoundLimiter was not injected. Check that FillLifetimeScope registers SoundLimiter and FlyingCubeArrivalSound.");
            }

            if (_minPitch > _maxPitch)
            {
                throw new InvalidOperationException(
                    $"{name}: MinPitch {_minPitch} is greater than MaxPitch {_maxPitch}.");
            }

            _filler = GetComponent<ShapeFiller>();
        }

        private void OnEnable()
        {
            _filler.CubeArrived += OnFillerCubeArrived;
        }

        private void OnDisable()
        {
            _filler.CubeArrived -= OnFillerCubeArrived;
        }

        private void OnFillerCubeArrived(FlyingCube cube)
        {
            if (Time.time - _lastPlayedTime < _minInterval)
            {
                return;
            }

            if (_soundLimiter.TryPlay(_sfxClip.Clip.length) == false)
            {
                return;
            }

            _lastPlayedTime = Time.time;

            float pitch = Mathf.Lerp(_minPitch, _maxPitch, _filler.FillFraction);
            _sfxPlayer.Play(_sfxClip.Clip, _sfxClip.Volume, pitch);
        }
    }
}