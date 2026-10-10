using Collectables;
using Scriptables;
using System;
using UnityEngine;
using VContainer;
using Random = UnityEngine.Random;

namespace Audio
{
    public class PlayerPickupSound : MonoBehaviour
    {
        [SerializeField] private PlayerConfig _config;
        [SerializeField] private SfxClip _sfxClip;

        private IGameSoundPlayer _soundPlayer;
        private ItemCollector _itemCollector;

        [Inject]
        public void Construct(IGameSoundPlayer soundPlayer, ItemCollector itemCollector)
        {
            _soundPlayer = soundPlayer;
            _itemCollector = itemCollector;
        }

        private void Awake()
        {
            if (_soundPlayer == null)
            {
                throw new InvalidOperationException(
                    $"{name}: IGameSoundPlayer was not injected. GameLifetimeScope must be the first object in the scene hierarchy.");
            }

            if (_config == null)
            {
                throw new InvalidOperationException(
                    $"{name}: PlayerConfig is not assigned. Drag the PlayerConfig asset into the _config field.");
            }

            if (_itemCollector == null)
            {
                throw new InvalidOperationException(
                    $"{name}: ItemCollector was not injected. Check that GameLifetimeScope registers ItemCollector and PlayerPickupSound.");
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
            _itemCollector.ItemCollected += OnItemCollected;
        }

        private void OnDisable()
        {
            _itemCollector.ItemCollected -= OnItemCollected;
        }

        private void OnItemCollected(Items.Item item)
        {
            float throttleSeconds = Random.Range(
                _config.PickupSoundMinInterval,
                _config.PickupSoundMaxInterval);

            _soundPlayer.Play(_sfxClip, throttleSeconds);
        }
    }
}
