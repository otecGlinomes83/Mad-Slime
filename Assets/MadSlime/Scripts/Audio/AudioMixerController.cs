using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Game;
using UnityEngine;
using UnityEngine.Audio;
using VContainer;

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
        private PlayerProgress _progress;

        [Inject]
        public void Construct(PlayerProgress progress)
        {
            _progress = progress;
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

            if (_progress == null)
            {
                throw new InvalidOperationException(
                    $"{name}: PlayerProgress was not injected. Check that ProjectLifetimeScope registers PlayerProgress and AudioMixerController.");
            }
        }

        private void OnEnable()
        {
            _progress.Ready += OnSavesLoaded;
            ApplyFromSaves();
        }

        private void OnDisable()
        {
            _progress.Ready -= OnSavesLoaded;
        }

        private void OnDestroy()
        {
            bool hasPendingSave = _saveCancellationTokenSource != null;

            CancelDelayedSave();

            if (hasPendingSave == true)
            {
                _progress.Save();
            }
        }

        private void OnSavesLoaded()
        {
            ApplyFromSaves();
        }

        private void ApplyFromSaves()
        {
            _musicVolume01 = _progress.MusicVolume;
            _sfxVolume01 = _progress.SfxVolume;

            ApplyMusic();
            ApplySFX();
        }

        public void SetMusicVolume(float volume01)
        {
            float clamped = Mathf.Clamp01(volume01);
            _musicVolume01 = clamped;
            _progress.MusicVolume = clamped;

            ApplyMusic();
            ScheduleSave();
        }

        public void SetSFXVolume(float volume01)
        {
            float clamped = Mathf.Clamp01(volume01);
            _sfxVolume01 = clamped;
            _progress.SfxVolume = clamped;

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

            _progress.Save();
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
