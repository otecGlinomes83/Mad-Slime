using Collectables;
using Scriptables;
using System;
using UnityEngine;
using VContainer;
using Random = UnityEngine.Random;

namespace Audio
{
    public sealed class PlayerPickupSound : MonoBehaviour
    {
        [SerializeField] private PlayerConfig _config;
        [SerializeField] private SfxClip _sfxClip;

        private SfxPlayer _sfxPlayer;
        private Collector _collector;
        private float _nextAllowedSoundTime;

        [Inject]
        public void Construct(SfxPlayer sfxPlayer, Collector collector)
        {
            _sfxPlayer = sfxPlayer;
            _collector = collector;
        }

        private void Awake()
        {
            if (_sfxPlayer == null)
            {
                throw new InvalidOperationException(
                    $"{name}: SfxPlayer was not injected. GameLifetimeScope must be the first object in the scene hierarchy.");
            }

            if (_config == null)
            {
                throw new InvalidOperationException(
                    $"{name}: PlayerConfig is not assigned. Drag the PlayerConfig asset into the _config field.");
            }

            if (_collector == null)
            {
                throw new InvalidOperationException(
                    $"{name}: Collector was not injected. Check that GameLifetimeScope registers Collector and PlayerPickupSound.");
            }

            if (_sfxClip == null)
            {
                throw new InvalidOperationException(
                    $"{name}: SfxClip is not assigned. Drag a SfxClip asset into the _sfxClip field.");
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

            PlayPop();
        }

        private void PlayPop()
        {
            float pitch = Random.Range(_config.PickupSoundMinPitch, _config.PickupSoundMaxPitch);
            _sfxPlayer.PlayGame(_sfxClip.Clip, _sfxClip.Volume, pitch);
        }
    }
}