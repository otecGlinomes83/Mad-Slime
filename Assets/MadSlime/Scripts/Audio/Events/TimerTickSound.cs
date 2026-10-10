using Scriptables;
using System;
using UnityEngine;
using VContainer;

namespace Audio
{
    public class TimerTickSound : MonoBehaviour
    {
        [SerializeField] private SfxClip _sfxClip;

        private IGameSoundPlayer _soundPlayer;
        private bool _isTickingActive;

        [Inject]
        public void Construct(IGameSoundPlayer soundPlayer)
        {
            _soundPlayer = soundPlayer;
        }

        private void Awake()
        {
            if (_soundPlayer == null)
            {
                throw new InvalidOperationException(
                    $"{name}: IGameSoundPlayer was not injected. GameLifetimeScope must be the first object in the scene hierarchy.");
            }

            if (_sfxClip == null)
            {
                throw new InvalidOperationException(
                    $"{name}: SfxClip is not assigned. Drag a SfxClip asset into the _sfxClip field.");
            }
        }

        private void OnDisable()
        {
            StopTicking();
        }

        public void StartTicking()
        {
            if (_isTickingActive == true)
            {
                return;
            }

            _soundPlayer.StartLoop(_sfxClip);
            _isTickingActive = true;
        }

        public void StopTicking()
        {
            if (_isTickingActive == false)
            {
                return;
            }

            _soundPlayer.StopLoop();
            _isTickingActive = false;
        }
    }
}
