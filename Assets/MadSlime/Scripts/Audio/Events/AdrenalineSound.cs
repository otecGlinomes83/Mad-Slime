using Player;
using Scriptables;
using System;
using UnityEngine;
using VContainer;

namespace Audio
{
    public class AdrenalineSound : MonoBehaviour
    {
        [SerializeField] private SfxClip _sfxClip;

        private IGameSoundPlayer _soundPlayer;
        private AdrenalineBoost _adrenalineBoost;

        [Inject]
        public void Construct(IGameSoundPlayer soundPlayer, AdrenalineBoost adrenalineBoost)
        {
            _soundPlayer = soundPlayer;
            _adrenalineBoost = adrenalineBoost;
        }

        private void Awake()
        {
            if (_soundPlayer == null)
            {
                throw new InvalidOperationException(
                    $"{name}: IGameSoundPlayer was not injected. GameLifetimeScope must be the first object in the scene hierarchy.");
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

            _soundPlayer.Play(_sfxClip);
        }
    }
}
