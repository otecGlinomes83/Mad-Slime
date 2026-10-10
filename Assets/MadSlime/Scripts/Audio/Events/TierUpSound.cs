using Player;
using Scriptables;
using Skills;
using System;
using UnityEngine;
using VContainer;

namespace Audio
{
    public class TierUpSound : MonoBehaviour
    {
        [SerializeField] private SfxClip _sfxClip;

        private IGameSoundPlayer _soundPlayer;
        private PlayerTier _playerTier;

        [Inject]
        public void Construct(IGameSoundPlayer soundPlayer, PlayerTier playerTier)
        {
            _soundPlayer = soundPlayer;
            _playerTier = playerTier;
        }

        private void Awake()
        {
            if (_soundPlayer == null)
            {
                throw new InvalidOperationException(
                    $"{name}: IGameSoundPlayer was not injected. GameLifetimeScope must be the first object in the scene hierarchy.");
            }

            if (_playerTier == null)
            {
                throw new InvalidOperationException(
                    $"{name}: PlayerTier was not injected. Check that GameLifetimeScope registers PlayerTier and TierUpSound.");
            }

            if (_sfxClip == null)
            {
                throw new InvalidOperationException(
                    $"{name}: SfxClip is not assigned. Drag a SfxClip asset into the _sfxClip field.");
            }
        }

        private void OnEnable()
        {
            _playerTier.TierChanged += OnTierChanged;
        }

        private void OnDisable()
        {
            _playerTier.TierChanged -= OnTierChanged;
        }

        private void OnTierChanged(SizeTier previousTier, SizeTier currentTier)
        {
            if (currentTier <= previousTier)
            {
                return;
            }

            _soundPlayer.Play(_sfxClip);
        }
    }
}