using System;
using ShapeFill;
using UnityEngine;
using UnityEngine.Audio;

namespace Audio
{
    [RequireComponent(typeof(AudioSource))]
    [RequireComponent(typeof(FlyingCube))]
    public sealed class FlyingCubeSound : MonoBehaviour
    {
        [SerializeField] private AudioMixerGroup _group;
        [SerializeField] private AudioClip _clip;

        private AudioSource _source;
        private FlyingCube _cube;

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

            _cube = GetComponent<FlyingCube>();
        }

        private void OnEnable()
        {
            _cube.Arrived += OnCubeArrived;
        }

        private void OnDisable()
        {
            _cube.Arrived -= OnCubeArrived;
        }

        private void OnCubeArrived(FlyingCube cube)
        {
            _source.PlayOneShot(_clip);
        }
    }
}