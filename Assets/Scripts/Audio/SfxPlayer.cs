using Scriptables;
using System;
using UnityEngine;
using UnityEngine.Audio;
using Random = UnityEngine.Random;

namespace Audio
{
    public sealed class SfxPlayer : MonoBehaviour
    {
        [Tooltip("Группа микшера, в которую отправляются одношотовые звуки")]
        [SerializeField] private AudioMixerGroup _sfxGroup;

        [Tooltip("Размер пула источников: сколько звуков может играть одновременно")]
        [SerializeField, Min(1)] private int _sourceCount = 7;

        private AudioSource[] _sources;
        private AudioSource _loopSource;
        private int _nextSourceIndex;

        private void Awake()
        {
            if (_sfxGroup == null)
            {
                throw new InvalidOperationException(
                    $"{name}: SFX mixer group is not assigned. Open the ProjectScope prefab and drag the SFX group of the MasterMixer into the Sfx Group field.");
            }

            _sources = new AudioSource[_sourceCount];

            for (int index = 0; index < _sourceCount; index++)
            {
                _sources[index] = CreateSource();
            }

            _loopSource = CreateSource();
            _loopSource.loop = true;
        }

        public void Play(SfxClip sfxClip)
        {
            ValidateSfxClip(sfxClip);

            float pitch;
            if (sfxClip.IsRandomPitch == true)
            {
                pitch = Random.Range(sfxClip.MinPitch, sfxClip.MaxPitch);
            }
            else
            {
                pitch = 1f;
            }

            PlayOneShot(sfxClip.Clip, sfxClip.Volume, pitch);
        }

        public void Play(AudioClip clip, float volume, float pitch)
        {
            if (clip == null)
            {
                throw new ArgumentNullException(nameof(clip), $"{name}: AudioClip is not assigned.");
            }

            PlayOneShot(clip, volume, pitch);
        }

        public void StartLoop(SfxClip sfxClip)
        {
            ValidateSfxClip(sfxClip);

            float pitch;
            if (sfxClip.IsRandomPitch == true)
            {
                pitch = Random.Range(sfxClip.MinPitch, sfxClip.MaxPitch);
            }
            else
            {
                pitch = 1f;
            }

            _loopSource.clip = sfxClip.Clip;
            _loopSource.volume = sfxClip.Volume;
            _loopSource.pitch = pitch;
            _loopSource.Play();
        }

        public void StopLoop()
        {
            if (_loopSource.isPlaying == true)
            {
                _loopSource.Stop();
            }
        }

        private void PlayOneShot(AudioClip clip, float volume, float pitch)
        {
            AudioSource source = _sources[_nextSourceIndex];
            _nextSourceIndex = (_nextSourceIndex + 1) % _sourceCount;

            source.pitch = pitch;
            source.volume = volume;
            source.PlayOneShot(clip);
        }

        private AudioSource CreateSource()
        {
            AudioSource source = gameObject.AddComponent<AudioSource>();
            source.outputAudioMixerGroup = _sfxGroup;
            source.playOnAwake = false;
            source.loop = false;
            source.spatialBlend = 0f;

            return source;
        }

        private void ValidateSfxClip(SfxClip sfxClip)
        {
            if (sfxClip == null)
            {
                throw new ArgumentNullException(nameof(sfxClip), $"{name}: SfxClip is not assigned.");
            }

            if (sfxClip.Clip == null)
            {
                throw new InvalidOperationException(
                    $"{name}: SfxClip '{sfxClip.name}' has no AudioClip assigned.");
            }

            if (sfxClip.IsRandomPitch == true && sfxClip.MinPitch > sfxClip.MaxPitch)
            {
                throw new InvalidOperationException(
                    $"{name}: SfxClip '{sfxClip.name}' has MinPitch {sfxClip.MinPitch} greater than MaxPitch {sfxClip.MaxPitch}.");
            }
        }
    }
}