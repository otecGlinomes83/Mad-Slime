using System;
using Player;
using Skills;
using UnityEngine;
using UnityEngine.Audio;

namespace Audio
{
    public sealed class TierUpSound : MonoBehaviour
    {
        [SerializeField] private PlayerTier _playerTier;
        [SerializeField] private AudioMixerGroup _group;
        [SerializeField] private AudioClip _clip;
        [SerializeField, Range(0f, 1f)] private float _volume = 1f;

        private AudioSource _source;

        private void Awake()
        {
            if (_playerTier == null)
            {
                throw new InvalidOperationException(
                    $"{name}: PlayerTier is not assigned. Drag a PlayerTier component into the _playerTier field.");
            }

            _source = gameObject.AddComponent<AudioSource>();
            _source.outputAudioMixerGroup = _group;
            _source.playOnAwake = false;
        }

        private void OnEnable()
        {
            _playerTier.TierChanged += OnTierChanged;
        }

        private void OnDisable()
        {
            _playerTier.TierChanged -= OnTierChanged;
        }

        private void OnTierChanged(ItemTier previousTier, ItemTier currentTier)
        {
            if (currentTier <= previousTier)
            {
                return;
            }

            if (_clip == null)
            {
                return;
            }

            _source.PlayOneShot(_clip, _volume);
        }
    }
}
