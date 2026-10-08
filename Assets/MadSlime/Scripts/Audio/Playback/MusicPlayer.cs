using Scriptables;
using System;
using UnityEngine;
using UnityEngine.Audio;

namespace Audio
{
    public sealed class MusicPlayer : MonoBehaviour
    {
        [Tooltip("Группа микшера, в которую отправляется музыка")]
        [SerializeField] private AudioMixerGroup _musicGroup;

        private AudioSource _source;

        private void Awake()
        {
            if (_musicGroup == null)
            {
                throw new InvalidOperationException(
                    $"{name}: Music mixer group is not assigned. Open the ProjectScope prefab and drag the Music group of the MasterMixer into the Music Group field.");
            }

            _source = gameObject.AddComponent<AudioSource>();
            _source.outputAudioMixerGroup = _musicGroup;
            _source.playOnAwake = false;
            _source.loop = true;
        }

        public void Play(SfxClip musicClip)
        {
            if (musicClip == null)
            {
                throw new ArgumentNullException(nameof(musicClip), $"{name}: SfxClip is not assigned.");
            }

            if (musicClip.Clip == null)
            {
                throw new InvalidOperationException(
                    $"{name}: SfxClip '{musicClip.name}' has no AudioClip assigned.");
            }

            if (_source.isPlaying == true && _source.clip == musicClip.Clip)
            {
                return;
            }

            _source.volume = musicClip.Volume;
            _source.pitch = 1f;
            _source.clip = musicClip.Clip;
            _source.Play();
        }
    }
}