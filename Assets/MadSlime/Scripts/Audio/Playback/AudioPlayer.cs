using Scriptables;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using Random = UnityEngine.Random;

namespace Audio
{
    public class AudioPlayer : MonoBehaviour, IMusicPlayer, IGameSoundPlayer, IUISoundPlayer
    {
        [Tooltip("Группа микшера, в которую отправляются звуки интерфейса и игры")]
        [SerializeField] private AudioMixerGroup _sfxGroup;

        [Tooltip("Группа микшера, в которую отправляется музыка")]
        [SerializeField] private AudioMixerGroup _musicGroup;

        [Tooltip("Голосов пула UI: сколько звуков интерфейса может играть одновременно на старте")]
        [SerializeField, Min(1)] private int _uiSourceCount = 2;

        [Tooltip("Голосов пула игры: сколько игровых звуков может играть одновременно на старте")]
        [SerializeField, Min(1)] private int _gameSourceCount = 5;

        [Tooltip("Длительность нарастания громкости музыки от нуля (с).")]
        [SerializeField, Min(0.01f)] private float _musicFadeInDuration = 1f;

        [Tooltip("Длительность затухания уходящего трека (с).")]
        [SerializeField, Min(0.01f)] private float _musicFadeOutDuration = 1.5f;

        [Tooltip("За сколько до конца трека начинается перекрытие со следующим (с).")]
        [SerializeField, Min(0.01f)] private float _musicOverlapDuration = 2.5f;

        private AudioVoicePool _uiPool;
        private AudioVoicePool _gamePool;
        private AudioSource _loopSource;
        private MusicChannel _musicChannel;
        private Dictionary<int, float> _nextAllowedPlayTimeByClipId = new Dictionary<int, float>();

        private void Awake()
        {
            if (_sfxGroup == null)
            {
                throw new InvalidOperationException(
                    $"{name}: SFX mixer group is not assigned. Open the ProjectScope prefab and drag "
                        + $"the SFX group of the MasterMixer into the Sfx Group field.");
            }

            if (_musicGroup == null)
            {
                throw new InvalidOperationException(
                    $"{name}: Music mixer group is not assigned. Open the ProjectScope prefab and "
                        + $"drag the Music group of the MasterMixer into the Music Group field.");
            }

            _uiPool = new AudioVoicePool(_uiSourceCount, _sfxGroup, transform);
            _gamePool = new AudioVoicePool(_gameSourceCount, _sfxGroup, transform);

            _loopSource = CreateSource(_sfxGroup);
            _loopSource.loop = true;

            AudioSource firstMusicSource = CreateMusicSource();
            AudioSource secondMusicSource = CreateMusicSource();

            _musicChannel = new MusicChannel(
                firstMusicSource,
                secondMusicSource,
                _musicFadeInDuration,
                _musicFadeOutDuration,
                _musicOverlapDuration);
        }

        private void Update()
        {
            _musicChannel.Tick(Time.unscaledDeltaTime);
        }

        public void Play(Playlist playlist)
        {
            _musicChannel.Play(playlist);
        }

        public void Play(SfxClip clip)
        {
            ValidateSfxClip(clip);
            _uiPool.Play(clip.Clip, clip.Volume, ResolvePitch(clip));
        }

        public void Play(SfxClip clip, float throttleSeconds)
        {
            if (PassesThrottle(clip, throttleSeconds) == false)
            {
                return;
            }

            ValidateSfxClip(clip);
            _gamePool.Play(clip.Clip, clip.Volume, ResolvePitch(clip));
        }

        public void Play(SfxClip clip, float pitch, float throttleSeconds)
        {
            if (PassesThrottle(clip, throttleSeconds) == false)
            {
                return;
            }

            ValidateSfxClip(clip);
            _gamePool.Play(clip.Clip, clip.Volume, pitch);
        }

        public void StartLoop(SfxClip clip)
        {
            ValidateSfxClip(clip);

            _loopSource.clip = clip.Clip;
            _loopSource.volume = clip.Volume;
            _loopSource.pitch = 1f;
            _loopSource.Play();
        }

        public void StopLoop()
        {
            if (_loopSource.isPlaying == true)
            {
                _loopSource.Stop();
            }
        }

        private AudioSource CreateMusicSource()
        {
            AudioSource source = CreateSource(_musicGroup);
            source.loop = false;

            return source;
        }

        private AudioSource CreateSource(AudioMixerGroup mixerGroup)
        {
            AudioSource source = gameObject.AddComponent<AudioSource>();
            source.outputAudioMixerGroup = mixerGroup;
            source.playOnAwake = false;
            source.loop = false;
            source.spatialBlend = 0f;

            return source;
        }

        private bool PassesThrottle(SfxClip clip, float throttleSeconds)
        {
            int clipId = clip.GetInstanceID();
            float currentTime = Time.unscaledTime;

            if (_nextAllowedPlayTimeByClipId.TryGetValue(clipId, out float nextAllowedTime) == true)
            {
                if (currentTime < nextAllowedTime)
                {
                    return false;
                }
            }

            _nextAllowedPlayTimeByClipId[clipId] = currentTime + throttleSeconds;

            return true;
        }

        private float ResolvePitch(SfxClip clip)
        {
            if (clip.IsRandomPitch == false)
            {
                return 1f;
            }

            return Random.Range(clip.MinPitch, clip.MaxPitch);
        }

        private void ValidateSfxClip(SfxClip clip)
        {
            if (clip == null)
            {
                throw new ArgumentNullException(nameof(clip), $"{name}: SfxClip is not assigned.");
            }

            if (clip.Clip == null)
            {
                throw new InvalidOperationException(
                    $"{name}: SfxClip '{clip.name}' has no AudioClip assigned.");
            }

            if (clip.IsRandomPitch && clip.MinPitch > clip.MaxPitch)
            {
                throw new InvalidOperationException(
                    $"{name}: SfxClip '{clip.name}' has MinPitch {clip.MinPitch} greater than MaxPitch {clip.MaxPitch}.");
            }
        }

    }
}
