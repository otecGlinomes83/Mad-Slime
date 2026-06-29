using System;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

namespace Audio
{
    [RequireComponent(typeof(AudioSource))]
    [RequireComponent(typeof(Button))]
    public sealed class UIButtonSound : MonoBehaviour
    {
        [SerializeField] private AudioMixerGroup _group;
        [SerializeField] private AudioClip _clip;

        private AudioSource _source;
        private Button _button;

        private void Awake()
        {
            if (_clip == null)
            {
                throw new InvalidOperationException(
                    $"{name}: AudioClip is not assigned.");
            }

            _source = GetComponent<AudioSource>();
            _source.outputAudioMixerGroup = _group;
            _source.playOnAwake = false;

            _button = GetComponent<Button>();
        }

        private void OnEnable()
        {
            _button.onClick.AddListener(PlayClick);
        }

        private void OnDisable()
        {
            _button.onClick.RemoveListener(PlayClick);
        }

        private void PlayClick()
        {
            _source.PlayOneShot(_clip);
        }
    }
}