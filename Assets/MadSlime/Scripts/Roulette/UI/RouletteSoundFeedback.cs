using Audio;
using Scriptables;
using UnityEngine;

namespace Roulette
{
    public class RouletteSoundFeedback
    {
        private const float MinTickIntervalSeconds = 0.06f;

        private RouletteWheel _wheel;
        private RouletteConfig _config;
        private IUISoundPlayer _soundPlayer;
        private float _lastTickTime;

        public void Setup(RouletteWheel wheel, RouletteConfig config, IUISoundPlayer soundPlayer)
        {
            _wheel = wheel;
            _config = config;
            _soundPlayer = soundPlayer;
            _wheel.Tick += OnTick;
        }

        public void Release()
        {
            _wheel.Tick -= OnTick;
        }

        public void PlaySpin()
        {
            Play(_config.SpinStartClip);
        }

        public void PlayWin()
        {
            Play(_config.WinClip);
        }

        private void OnTick()
        {
            if (Time.unscaledTime - _lastTickTime < MinTickIntervalSeconds)
            {
                return;
            }

            _lastTickTime = Time.unscaledTime;
            Play(_config.StepClip);
        }

        private void Play(SfxClip clip)
        {
            if (clip != null)
            {
                _soundPlayer.Play(clip);
            }
        }
    }
}
