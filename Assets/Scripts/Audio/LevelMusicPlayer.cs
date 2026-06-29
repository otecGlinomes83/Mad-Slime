using System;
using UnityEngine;
using UnityEngine.Audio;

namespace Audio
{
    [RequireComponent(typeof(AudioSource))]
    public sealed class LevelMusicPlayer : MonoBehaviour
    {
        [SerializeField] private AudioMixerGroup _group;
        [SerializeField] private AudioClip _clip;

        private AudioSource _source;

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
            _source.loop = true;
            _source.clip = _clip;
        }

        private void Start()
        {
            _source.Play();
        }
    }
}