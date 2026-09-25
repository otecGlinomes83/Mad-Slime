using Audio;
using PlayerInput;
using Scriptables;
using System;
using UnityEngine;
using VContainer;
using YG;

namespace Game
{
    public sealed class GameplaySessionHandler : MonoBehaviour
    {
        [SerializeField] private SfxClip _musicTrack;

        private LevelConfigResolver _configResolver;
        private MusicPlayer _musicPlayer;
        private PlayerProgress _progress;
        private LevelProgress _levelProgress;
        private LevelTransitor _levelTransitor;
        private Timer _timer;
        private PlayerInputReader _inputReader;
        private Pauser _pauser;

        private bool _isStarted;
        private bool _isFinished;
        private bool _isSubscribed;

        [Inject]
        public void Construct(LevelConfigResolver configResolver, PlayerProgress progress, LevelProgress levelProgress,
            MusicPlayer musicPlayer, LevelTransitor levelTransitor, Timer timer, PlayerInputReader inputReader, Pauser pauser)
        {
            _configResolver = configResolver;
            _progress = progress;
            _levelProgress = levelProgress;
            _musicPlayer = musicPlayer;
            _levelTransitor = levelTransitor;
            _timer = timer;
            _inputReader = inputReader;
            _pauser = pauser;
        }

        private void Awake()
        {
            if (_configResolver == null)
            {
                throw new InvalidOperationException(
                    $"{name}: dependencies were not injected. GameLifetimeScope must be the first object in the scene hierarchy.");
            }

            if (_musicPlayer == null)
            {
                throw new InvalidOperationException(
                    $"{name}: MusicPlayer was not injected. GameLifetimeScope must be the first object in the scene hierarchy.");
            }

            if (_musicTrack == null)
            {
                throw new InvalidOperationException(
                    $"{name}: Music track is not assigned. Drag a SfxClip asset into the _musicTrack field.");
            }

            LevelConfig config = _configResolver.GetConfigFor(_progress.CurrentLevel);

            _timer.Setup(config.TimerDuration);
            _pauser.RequestPause();
        }

        private void OnEnable()
        {
            if (_levelProgress == null)
            {
                return;
            }

            SubscribeSession();
        }

        private void Start()
        {
            if (_levelProgress == null)
            {
                throw new InvalidOperationException(
                    $"{name}: LevelProgress was not injected. Check that GameLifetimeScope is configured and Player is registered.");
            }

            _musicPlayer.Play(_musicTrack);
            SubscribeSession();
        }

        private void OnDisable()
        {
            _isSubscribed = false;

            _inputReader.MovementKeyPressed -= Begin;
            _timer.Finished -= OnTimeOut;

            if (_levelProgress != null)
            {
                _levelProgress.QuotaCompleted -= OnQuotaCompleted;
            }
        }

        private void SubscribeSession()
        {
            if (_isSubscribed == true)
            {
                return;
            }

            _isSubscribed = true;

            _inputReader.MovementKeyPressed += Begin;
            _timer.Finished += OnTimeOut;
            _levelProgress.QuotaCompleted += OnQuotaCompleted;
        }

        public void ExitToMenu()
        {
            StopGameplay();
            _levelTransitor.LoadMenu();
        }

        private void StopGameplay()
        {
            if (_isStarted == false)
            {
                return;
            }

            YG2.GameplayStop();
        }

        private void Begin()
        {
            if (_isStarted == true || _isFinished == true)
            {
                return;
            }

            _isStarted = true;

            _pauser.RequestResume();
            _timer.StartCount();
            YG2.GameplayStart();
        }

        private void OnTimeOut()
        {
            if (_isFinished == true)
            {
                return;
            }

            _isFinished = true;
            FinishGame("timeout");
        }

        private void OnQuotaCompleted()
        {
            if (_isFinished == true)
            {
                return;
            }

            _isFinished = true;
            FinishGame("quota completed");
        }

        private void FinishGame(string reason)
        {
            Debug.Log(
                $"[Game] finished: {reason} | Level={_progress.CurrentLevel} Quota {_levelProgress.CollectedQuotaCount}/{_levelProgress.TotalQuotaTarget} Fill={_levelProgress.FillPercent:0.00}");

            _timer.Stop();
            YG2.GameplayStop();

            _levelTransitor.LoadFill();
        }
    }
}
