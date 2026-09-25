using Scriptables;
using System;
using UnityEngine;
using UnityEngine.Audio;
using Random = UnityEngine.Random;

namespace Audio
{
    public sealed class SfxPlayer : MonoBehaviour
    {
        [Tooltip("Группа микшера, в которую отправляются звуки интерфейса и игры")]
        [SerializeField] private AudioMixerGroup _sfxGroup;

        [Tooltip("Голосов пула UI: сколько звуков интерфейса может играть одновременно")]
        [SerializeField, Min(1)] private int _uiSourceCount = 2;

        [Tooltip("Голосов игрового пула: сколько игровых звуков может играть одновременно")]
        [SerializeField, Min(1)] private int _gameSourceCount = 5;

        private VoicePool _uiPool;
        private VoicePool _gamePool;
        private AudioSource _loopSource;

        private void Awake()
        {
            if (_sfxGroup == null)
            {
                throw new InvalidOperationException(
                    $"{name}: SFX mixer group is not assigned. Open the ProjectScope prefab and drag the SFX group of the MasterMixer into the Sfx Group field.");
            }

            _uiPool = new VoicePool(CreateSources(_uiSourceCount));
            _gamePool = new VoicePool(CreateSources(_gameSourceCount));

            _loopSource = CreateSource();
            _loopSource.loop = true;
        }

        public void PlayUi(SfxClip sfxClip)
        {
            ValidateSfxClip(sfxClip);
            _uiPool.Play(sfxClip.Clip, sfxClip.Volume, ResolvePitch(sfxClip));
        }

        public void PlayGame(SfxClip sfxClip)
        {
            ValidateSfxClip(sfxClip);
            _gamePool.Play(sfxClip.Clip, sfxClip.Volume, ResolvePitch(sfxClip));
        }

        public void PlayGame(AudioClip clip, float volume, float pitch)
        {
            if (clip == null)
            {
                throw new ArgumentNullException(nameof(clip), $"{name}: AudioClip is not assigned.");
            }

            _gamePool.Play(clip, volume, pitch);
        }

        public void StartLoop(SfxClip sfxClip)
        {
            ValidateSfxClip(sfxClip);

            _loopSource.clip = sfxClip.Clip;
            _loopSource.volume = sfxClip.Volume;
            _loopSource.pitch = ResolvePitch(sfxClip);
            _loopSource.Play();
        }

        public void StopLoop()
        {
            if (_loopSource.isPlaying == true)
            {
                _loopSource.Stop();
            }
        }

        private AudioSource[] CreateSources(int sourceCount)
        {
            AudioSource[] sources = new AudioSource[sourceCount];

            for (int index = 0; index < sourceCount; index++)
            {
                sources[index] = CreateSource();
            }

            return sources;
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

        private float ResolvePitch(SfxClip sfxClip)
        {
            if (sfxClip.IsRandomPitch == false)
            {
                return 1f;
            }

            return Random.Range(sfxClip.MinPitch, sfxClip.MaxPitch);
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

        private sealed class VoicePool
        {
            private readonly AudioSource[] _sources;
            private readonly float[] _sourceEndTimes;
            private int _nextSourceIndex;

            public VoicePool(AudioSource[] sources)
            {
                _sources = sources;
                _sourceEndTimes = new float[sources.Length];
            }

            public void Play(AudioClip clip, float volume, float pitch)
            {
                if (CountActiveVoices() >= _sources.Length)
                {
                    return;
                }

                int sourceIndex = _nextSourceIndex;
                _nextSourceIndex = (_nextSourceIndex + 1) % _sources.Length;

                AudioSource source = _sources[sourceIndex];
                source.pitch = pitch;
                source.volume = volume;
                source.PlayOneShot(clip);
                _sourceEndTimes[sourceIndex] = Time.time + clip.length / pitch;
            }

            private int CountActiveVoices()
            {
                int activeCount = 0;

                for (int index = 0; index < _sources.Length; index++)
                {
                    if (Time.time < _sourceEndTimes[index])
                    {
                        activeCount++;
                    }
                }

                return activeCount;
            }
        }
    }
}
