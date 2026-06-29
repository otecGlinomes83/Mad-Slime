using System;
using UnityEngine;
using UnityEngine.Audio;
using YG;

namespace Audio
{
    public sealed class AudioMixerController : MonoBehaviour
    {
        private const string MusicVolumeParam = "MusicVolume";
        private const string SfxVolumeParam = "SFXVolume";
        private const float MinLinearGuard = 0.0001f;

        [SerializeField] private AudioMixer _mixer;
        [SerializeField] private AudioMixerGroup _musicGroup;
        [SerializeField] private AudioMixerGroup _sfxGroup;

        private float _musicVolume01;
        private float _sfxVolume01;
        private bool _isReady;

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
                    $"{name}: Music AudioMixerGroup is not assigned.");
            }

            if (_sfxGroup == null)
            {
                throw new InvalidOperationException(
                    $"{name}: SFX AudioMixerGroup is not assigned.");
            }

            _musicVolume01 = YG2.saves.musicVolume;
            _sfxVolume01 = YG2.saves.sfxVolume;
        }

        private void OnEnable()
        {
            YG2.onGetSDKData += OnSavesLoaded;
            TryApplyFromSaves();
        }

        private void OnDisable()
        {
            YG2.onGetSDKData -= OnSavesLoaded;
        }

        private void OnSavesLoaded()
        {
            TryApplyFromSaves();
        }

        private void TryApplyFromSaves()
        {
            if (YG2.isSDKEnabled == false)
            {
                return;
            }

            _musicVolume01 = YG2.saves.musicVolume;
            _sfxVolume01 = YG2.saves.sfxVolume;

            ApplyMusic();
            ApplySFX();

            _isReady = true;
        }

        public void SetMusicVolume(float volume01)
        {
            if (_isReady == false)
            {
                return;
            }

            float clamped = Mathf.Clamp01(volume01);
            _musicVolume01 = clamped;
            YG2.saves.musicVolume = clamped;

            ApplyMusic();
            YG2.SaveProgress();
        }

        public void SetSFXVolume(float volume01)
        {
            if (_isReady == false)
            {
                return;
            }

            float clamped = Mathf.Clamp01(volume01);
            _sfxVolume01 = clamped;
            YG2.saves.sfxVolume = clamped;

            ApplySFX();
            YG2.SaveProgress();
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