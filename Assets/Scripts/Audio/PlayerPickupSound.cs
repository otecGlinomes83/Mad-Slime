using Collectables;
using Items;
using Scriptables;
using System;
using UnityEngine;
using UnityEngine.Audio;
using Random = UnityEngine.Random;

namespace Audio
{
    [RequireComponent(typeof(AudioSource))]
    public sealed class PlayerPickupSound : MonoBehaviour
    {
        [SerializeField] private PlayerConfig _config;
        [SerializeField] private Collector _collector;
        [SerializeField] private SoundLimiter _soundLimiter;
        [SerializeField] private AudioMixerGroup _group;
        [SerializeField] private AudioClip _clip;

        private AudioSource _source;
        private float _nextAllowedSoundTime;

        private void Awake()
        {
            if (_config == null)
            {
                throw new InvalidOperationException(
                    $"{name}: PlayerConfig is not assigned. Drag the PlayerConfig asset into the _config field.");
            }

            if (_collector == null)
            {
                throw new InvalidOperationException(
                    $"{name}: Collector is not assigned. Drag a Collector component into the _collector field.");
            }

            if (_soundLimiter == null)
            {
                throw new InvalidOperationException(
                    $"{name}: SoundLimiter is not assigned. Drag a SoundLimiter component into the _soundLimiter field.");
            }

            if (_clip == null)
            {
                throw new InvalidOperationException(
                    $"{name}: AudioClip is not assigned.");
            }

            if (_config.PickupSoundMinInterval > _config.PickupSoundMaxInterval)
            {
                throw new InvalidOperationException(
                    $"{name}: PlayerConfig has PickupSoundMinInterval {_config.PickupSoundMinInterval} greater than PickupSoundMaxInterval {_config.PickupSoundMaxInterval}.");
            }

            if (_config.PickupSoundMinPitch > _config.PickupSoundMaxPitch)
            {
                throw new InvalidOperationException(
                    $"{name}: PlayerConfig has PickupSoundMinPitch {_config.PickupSoundMinPitch} greater than PickupSoundMaxPitch {_config.PickupSoundMaxPitch}.");
            }

            _source = GetComponent<AudioSource>();
            _source.outputAudioMixerGroup = _group;
            _source.playOnAwake = false;
        }

        private void OnEnable()
        {
            _collector.ItemCollected += OnItemCollected;
        }

        private void OnDisable()
        {
            _collector.ItemCollected -= OnItemCollected;
        }

        private void OnItemCollected(Items.Item item)
        {
            if (Time.time < _nextAllowedSoundTime)
            {
                return;
            }

            _nextAllowedSoundTime = Time.time + Random.Range(
                _config.PickupSoundMinInterval,
                _config.PickupSoundMaxInterval);

            if (_soundLimiter.TryPlay(_clip.length) == false)
            {
                return;
            }

            PlayPop();
        }

        private void PlayPop()
        {
            _source.pitch = Random.Range(_config.PickupSoundMinPitch, _config.PickupSoundMaxPitch);
            _source.PlayOneShot(_clip);
        }
    }
}
