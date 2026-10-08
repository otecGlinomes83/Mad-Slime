using Player;
using Scriptables;
using System;
using UnityEngine;
using VContainer;

namespace Audio
{
    public sealed class AdrenalineSound : MonoBehaviour
    {
        [SerializeField] private SfxClip _sfxClip;

        private SfxPlayer _sfxPlayer;
        private AdrenalineBoost _adrenalineBoost;

        [Inject]
        public void Construct(SfxPlayer sfxPlayer, AdrenalineBoost adrenalineBoost)
        {
            _sfxPlayer = sfxPlayer;
            _adrenalineBoost = adrenalineBoost;
        }

        private void Awake()
        {
            if (_sfxPlayer == null)
            {
                throw new InvalidOperationException(
                    $"{name}: SfxPlayer was not injected. GameLifetimeScope must be the first object in the scene hierarchy.");
            }

            if (_adrenalineBoost == null)
            {
                throw new InvalidOperationException(
                    $"{name}: AdrenalineBoost was not injected. Check that GameLifetimeScope registers AdrenalineBoost and AdrenalineSound.");
            }
        }

        private void OnEnable()
        {
            _adrenalineBoost.BoostStarted += OnBoostStarted;
        }

        private void OnDisable()
        {
            _adrenalineBoost.BoostStarted -= OnBoostStarted;
        }

        private void OnBoostStarted()
        {
            if (_sfxClip == null)
            {
                return;
            }

            _sfxPlayer.PlayGame(_sfxClip);
        }
    }
}
