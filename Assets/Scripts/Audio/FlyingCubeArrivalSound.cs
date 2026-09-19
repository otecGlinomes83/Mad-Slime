using System;
using ShapeFill;
using UnityEngine;
using UnityEngine.Audio;

namespace Audio
{
    [RequireComponent(typeof(AudioSource))]
    [RequireComponent(typeof(ShapeFiller))]
    public sealed class FlyingCubeArrivalSound : MonoBehaviour
    {
        [SerializeField] private AudioMixerGroup _group;

        [SerializeField] private AudioClip _clip;

        [SerializeField] private SoundLimiter _soundLimiter;

        [SerializeField, Range(0.5f, 2f)] private float _minPitch = 0.9f;

        [SerializeField, Range(0.5f, 2f)] private float _maxPitch = 1.35f;

        [SerializeField, Min(0f)] private float _minInterval = 0.03f;

        private AudioSource _source;
        private ShapeFiller _filler;
        private float _lastPlayedTime;

        private void Awake()
        {
            if (_clip == null)
            {
                throw new InvalidOperationException(
                    $"{name}: AudioClip is not assigned.");
            }

            if (_soundLimiter == null)
            {
                throw new InvalidOperationException(
                    $"{name}: SoundLimiter is not assigned. Drag a SoundLimiter component into the _soundLimiter field.");
            }

            if (_minPitch > _maxPitch)
            {
                throw new InvalidOperationException(
                    $"{name}: MinPitch {_minPitch} is greater than MaxPitch {_maxPitch}.");
            }

            _source = GetComponent<AudioSource>();
            _source.outputAudioMixerGroup = _group;
            _source.playOnAwake = false;
            _source.spatialBlend = 0f;

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

            if (_soundLimiter.TryPlay(_clip.length) == false)
            {
                return;
            }

            _lastPlayedTime = Time.time;
            _source.pitch = Mathf.Lerp(_minPitch, _maxPitch, _filler.FillFraction);
            _source.PlayOneShot(_clip);
        }
    }
}
