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

        private AudioSource _source;
        private ShapeFiller _filler;

        private void Awake()
        {
            if (_clip == null)
            {
                throw new InvalidOperationException(
                    $"{name}: AudioClip is not assigned.");
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
            _source.PlayOneShot(_clip);
        }
    }
}
