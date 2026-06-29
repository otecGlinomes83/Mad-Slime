using System;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

namespace Audio
{
    [RequireComponent(typeof(AudioSource))]
    public sealed class UIButtonSound : MonoBehaviour
    {
        [SerializeField] private AudioMixerGroup _group;
        [SerializeField] private AudioClip _clip;
        [SerializeField]private Button[] _buttons;

        private AudioSource _source;

        private void Awake()
        {
            if (_clip == null)
            {
                throw new InvalidOperationException(
                    $"{name}: AudioClip is not assigned.");
            }

            if (_buttons.Length <= 0)
            {
                throw new InvalidOperationException(
                    $"{name}: buttons is empty");
            }

            _source = GetComponent<AudioSource>();
            _source.outputAudioMixerGroup = _group;
            _source.playOnAwake = false;
        }

        private void OnEnable()
        {
            foreach (Button button in _buttons)
            {
                button.onClick.AddListener(PlayClick);
            }
        }

        private void OnDisable()
        {
            foreach (Button button in _buttons)
            {
                button.onClick.RemoveListener(PlayClick);
            }
        }

        private void PlayClick()
        {
            _source.PlayOneShot(_clip);
        }
    }
}