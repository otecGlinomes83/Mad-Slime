using System;
using UnityEngine;
using UnityEngine.UI;

namespace Audio
{
    public sealed class AudioSettingsPanel : MonoBehaviour
    {
        [SerializeField] private AudioMixerController _mixerController;
        [SerializeField] private Slider _musicSlider;
        [SerializeField] private Slider _sfxSlider;

        private bool _isInitialized;

        private void Awake()
        {
            if (_mixerController == null)
            {
                throw new InvalidOperationException(
                    $"{name}: AudioMixerController is not assigned.");
            }

            if (_musicSlider == null)
            {
                throw new InvalidOperationException(
                    $"{name}: Music Slider is not assigned.");
            }

            if (_sfxSlider == null)
            {
                throw new InvalidOperationException(
                    $"{name}: SFX Slider is not assigned.");
            }

            _musicSlider.minValue = 0f;
            _musicSlider.maxValue = 1f;
            _sfxSlider.minValue = 0f;
            _sfxSlider.maxValue = 1f;
        }

        private void OnEnable()
        {
            _musicSlider.value = _mixerController.MusicVolume;
            _sfxSlider.value = _mixerController.SFXVolume;

            _musicSlider.onValueChanged.AddListener(OnMusicSliderChanged);
            _sfxSlider.onValueChanged.AddListener(OnSfxSliderChanged);

            _isInitialized = true;
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

        private void OnMusicSliderChanged(float value)
        {
            _mixerController.SetMusicVolume(value);
        }

        private void OnSfxSliderChanged(float value)
        {
            _mixerController.SetSFXVolume(value);
        }
    }
}