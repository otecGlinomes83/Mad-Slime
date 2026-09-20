using Player;
using Scriptables;
using Skills;
using System;
using UnityEngine;
using VContainer;

namespace Audio
{
    public sealed class TierUpSound : MonoBehaviour
    {
        [SerializeField] private PlayerTier _playerTier;
        [SerializeField] private SfxClip _sfxClip;

        private SfxPlayer _sfxPlayer;

        [Inject]
        public void Construct(SfxPlayer sfxPlayer)
        {
            _sfxPlayer = sfxPlayer;
        }

        private void Awake()
        {
            if (_sfxPlayer == null)
            {
                throw new InvalidOperationException(
                    $"{name}: SfxPlayer was not injected. GameLifetimeScope must be the first object in the scene hierarchy.");
            }

            if (_playerTier == null)
            {
                throw new InvalidOperationException(
                    $"{name}: PlayerTier is not assigned. Drag a PlayerTier component into the _playerTier field.");
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

        private void OnTierChanged(ItemTier previousTier, ItemTier currentTier)
        {
            if (currentTier <= previousTier)
            {
                return;
            }

            _sfxPlayer.Play(_sfxClip);
        }
    }
}