using Saves;
using System;
using UnityEngine;
using UnityEngine.Audio;
using VContainer;

namespace Audio
{
    public class AudioMixerController : MonoBehaviour
    {
        private const string MusicVolumeParam = "MusicVolume";
        private const string SfxVolumeParam = "SFXVolume";
        private const float MinLinearGuard = 0.0001f;

        [SerializeField] private AudioMixer _mixer;
        [SerializeField] private AudioMixerGroup _musicGroup;
        [SerializeField] private AudioMixerGroup _sfxGroup;

        private IAudioStorage _audioStorage;
        private ISavesReadiness _savesReadiness;
        private float _musicVolume01;
        private float _sfxVolume01;
        private bool _isVolumesLoaded;
        private bool _hasUnsavedChanges;

        [Inject]
        public void Construct(IAudioStorage audioStorage, ISavesReadiness savesReadiness)
        {
            _audioStorage = audioStorage;
            _savesReadiness = savesReadiness;
        }

        public float MusicVolume => _musicVolume01;
        public float SFXVolume => _sfxVolume01;

        private void Awake()
        {
            if (_mixer == null)
            {
                throw new InvalidOperationException(
                    $"{name}: AudioMixer is not assigned. Drag MasterMixer into the _mixer field.");
            }

            if (_musicGroup == null)
            {
                throw new InvalidOperationException(
                    $"{name}: Music AudioMixerGroup is not assigned. Drag a group into the _musicGroup field.");
            }

            if (_sfxGroup == null)
            {
                throw new InvalidOperationException(
                    $"{name}: SFX AudioMixerGroup is not assigned. Drag a group into the _sfxGroup field.");
            }

            if (_audioStorage == null || _savesReadiness == null)
            {
                throw new InvalidOperationException(
                    $"{name}: saves dependencies were not injected. Check that ProjectLifetimeScope registers Saver and AudioMixerController.");
            }
        }

        private void OnEnable()
        {
            _savesReadiness.Ready += OnSavesReady;
            ApplyFromSaves();
        }

        private void OnDisable()
        {
            _savesReadiness.Ready -= OnSavesReady;
        }

        private void OnSavesReady()
        {
            ApplyFromSaves();
        }

        private void ApplyFromSaves()
        {
            if (_savesReadiness.IsReady == false)
            {
                return;
            }

            _musicVolume01 = _audioStorage.MusicVolume;
            _sfxVolume01 = _audioStorage.SfxVolume;
            _isVolumesLoaded = true;
            _hasUnsavedChanges = false;

            ApplyMusic();
            ApplySFX();
        }

        public void SetMusicVolume(float volume01)
        {
            _musicVolume01 = Mathf.Clamp01(volume01);
            _hasUnsavedChanges = true;

            ApplyMusic();
        }

        public void SetSFXVolume(float volume01)
        {
            _sfxVolume01 = Mathf.Clamp01(volume01);
            _hasUnsavedChanges = true;

            ApplySFX();
        }

        public void CommitVolumes()
        {
            if (_isVolumesLoaded == false || _hasUnsavedChanges == false)
            {
                return;
            }

            _hasUnsavedChanges = false;
            _audioStorage.SetVolumes(_musicVolume01, _sfxVolume01);
        }

        private void ApplyMusic()
        {
            float decibels = LinearToDecibels(_musicVolume01);
            _mixer.SetFloat(MusicVolumeParam, decibels);
        }

        private void ApplySFX()
        {
            float decibels = LinearToDecibels(_sfxVolume01);
            _mixer.SetFloat(SfxVolumeParam, decibels);
        }

        private static float LinearToDecibels(float volume01)
        {
            float guarded = Mathf.Max(volume01, MinLinearGuard);
            return Mathf.Log10(guarded) * 20f;
        }
    }
}
