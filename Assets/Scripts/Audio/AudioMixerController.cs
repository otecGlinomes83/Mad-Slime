using System;
using System.Threading;
using Cysharp.Threading.Tasks;
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
        private const int SaveDelayMilliseconds = 500;

        [SerializeField] private AudioMixer _mixer;
        [SerializeField] private AudioMixerGroup _musicGroup;
        [SerializeField] private AudioMixerGroup _sfxGroup;

        private float _musicVolume01;
        private float _sfxVolume01;
        private CancellationTokenSource _saveCancellationTokenSource;

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

            _musicVolume01 = YG2.saves.musicVolume;
            _sfxVolume01 = YG2.saves.sfxVolume;
        }

        private void OnEnable()
        {
            YG2.onGetSDKData += OnSavesLoaded;
            ApplyFromSaves();
        }

        private void OnDisable()
        {
            YG2.onGetSDKData -= OnSavesLoaded;
        }

        private void OnDestroy()
        {
            CancelDelayedSave();
        }

        private void OnSavesLoaded()
        {
            ApplyFromSaves();
        }

        private void ApplyFromSaves()
        {
            _musicVolume01 = YG2.saves.musicVolume;
            _sfxVolume01 = YG2.saves.sfxVolume;

            ApplyMusic();
            ApplySFX();
        }

        public void SetMusicVolume(float volume01)
        {
            float clamped = Mathf.Clamp01(volume01);
            _musicVolume01 = clamped;
            YG2.saves.musicVolume = clamped;

            ApplyMusic();
            ScheduleSave();
        }

        public void SetSFXVolume(float volume01)
        {
            float clamped = Mathf.Clamp01(volume01);
            _sfxVolume01 = clamped;
            YG2.saves.sfxVolume = clamped;

            ApplySFX();
            ScheduleSave();
        }

        private void ScheduleSave()
        {
            CancelDelayedSave();

            _saveCancellationTokenSource = new CancellationTokenSource();
            SaveDelayedAsync(_saveCancellationTokenSource.Token).Forget();
        }

        private async UniTaskVoid SaveDelayedAsync(CancellationToken cancellationToken)
        {
            try
            {
                await UniTask.Delay(SaveDelayMilliseconds, cancellationToken: cancellationToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            _saveCancellationTokenSource?.Dispose();
            _saveCancellationTokenSource = null;

            if (YG2.isSDKEnabled == true)
            {
                YG2.SaveProgress();
            }
        }

        private void CancelDelayedSave()
        {
            if (_saveCancellationTokenSource == null)
            {
                return;
            }

            _saveCancellationTokenSource.Cancel();
            _saveCancellationTokenSource.Dispose();
            _saveCancellationTokenSource = null;
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
