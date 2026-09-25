using System;
using Scriptables;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace Audio
{
    public sealed class AudioSettingsPanel : MonoBehaviour
    {
        private const float TickThrottleSeconds = 0.08f;

        [SerializeField] private Slider _musicSlider;
        [SerializeField] private Slider _sfxSlider;
        [SerializeField] private SfxClip _tickClip;

        private AudioMixerController _mixerController;
        private SfxPlayer _sfxPlayer;
        private float _lastTickTime;
        private bool _isInitialized;

        [Inject]
        public void Construct(AudioMixerController mixerController, SfxPlayer sfxPlayer)
        {
            _mixerController = mixerController;
            _sfxPlayer = sfxPlayer;
        }

        private void OnDisable()
        {
            if (_isInitialized == false)
            {
                return;
            }

            _musicSlider.onValueChanged.RemoveListener(OnMusicSliderChanged);
            _sfxSlider.onValueChanged.RemoveListener(OnSfxSliderChanged);

            _isInitialized = false;
        }

        public void Initialize()
        {
            if (_mixerController == null)
            {
                throw new InvalidOperationException(
                    $"{name}: AudioMixerController was not injected. The settings prefab must be instantiated through the DI container (IObjectResolver.Instantiate).");
            }

            if (_sfxPlayer == null)
            {
                throw new InvalidOperationException(
                    $"{name}: SfxPlayer was not injected. The settings prefab must be instantiated through the DI container (IObjectResolver.Instantiate).");
            }

            if (_musicSlider == null)
            {
                throw new InvalidOperationException(
                    $"{name}: Music Slider is not assigned. Drag a Slider into the _musicSlider field.");
            }

            if (_sfxSlider == null)
            {
                throw new InvalidOperationException(
                    $"{name}: SFX Slider is not assigned. Drag a Slider into the _sfxSlider field.");
            }

            if (_tickClip == null)
            {
                throw new InvalidOperationException(
                    $"{name}: Tick Clip is not assigned. Drag a SfxClip asset into the _tickClip field.");
            }

            _musicSlider.minValue = 0f;
            _sfxSlider.minValue = 0f;

            _musicSlider.maxValue = 1f;
            _sfxSlider.maxValue = 1f;

            _musicSlider.onValueChanged.RemoveListener(OnMusicSliderChanged);
            _sfxSlider.onValueChanged.RemoveListener(OnSfxSliderChanged);

            _musicSlider.value = _mixerController.MusicVolume;
            _sfxSlider.value = _mixerController.SFXVolume;

            _musicSlider.onValueChanged.AddListener(OnMusicSliderChanged);
            _sfxSlider.onValueChanged.AddListener(OnSfxSliderChanged);

            _isInitialized = true;
        }

        private void OnMusicSliderChanged(float value)
        {
            _mixerController.SetMusicVolume(value);
        }

        private void OnSfxSliderChanged(float value)
        {
            _mixerController.SetSFXVolume(value);
            PlayTick();
        }

        private void PlayTick()
        {
            float currentTime = Time.unscaledTime;

            if (currentTime - _lastTickTime < TickThrottleSeconds)
            {
                return;
            }

            _lastTickTime = currentTime;
            _sfxPlayer.PlayUi(_tickClip);
        }
    }
}