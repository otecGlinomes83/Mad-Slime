using Audio;
using Core;
using Cysharp.Threading.Tasks;
using PlayerInput;
using Scriptables;
using System;
using UnityEngine;
using VContainer;

namespace Game
{
    public sealed class GameplaySessionHandler : MonoBehaviour
    {
        private enum SessionState
        {
            WaitingForStart,
            Running,
            Finished
        }

        [SerializeField] private SfxClip _musicTrack;

        [Tooltip("Пауза перед переходом в Fill после конца сессии: игрок видит, что игра закончилась.")]
        [SerializeField, Min(0f)] private float _sessionEndDelay = 1f;

        private LevelConfigResolver _configResolver;
        private MusicPlayer _musicPlayer;
        private PlayerProgress _progress;
        private LevelProgress _levelProgress;
        private GameDirector _gameDirector;
        private Timer _timer;
        private PlayerInputReader _inputReader;
        private Pauser _pauser;
        private IGameplayReporter _gameplayReporter;

        private SessionState _state = SessionState.WaitingForStart;

        [Inject]
        public void Construct(LevelConfigResolver configResolver, PlayerProgress progress, LevelProgress levelProgress,
            MusicPlayer musicPlayer, GameDirector gameDirector, Timer timer, PlayerInputReader inputReader, Pauser pauser,
            IGameplayReporter gameplayReporter)
        {
            _configResolver = configResolver;
            _progress = progress;
            _levelProgress = levelProgress;
            _musicPlayer = musicPlayer;
            _gameDirector = gameDirector;
            _timer = timer;
            _inputReader = inputReader;
            _pauser = pauser;
            _gameplayReporter = gameplayReporter;
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

            if (_gameDirector == null)
            {
                throw new InvalidOperationException(
                    $"{name}: GameDirector was not injected. Check that ProjectLifetimeScope registers GameDirector and GameLifetimeScope registers GameplaySessionHandler.");
            }

            if (_gameplayReporter == null)
            {
                throw new InvalidOperationException(
                    $"{name}: IGameplayReporter was not injected. Check that ProjectLifetimeScope registers the YG2 gameplay adapter.");
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
                throw new InvalidOperationException(
                    $"{name}: LevelProgress was not injected. Check that GameLifetimeScope is configured and Player is registered.");
            }

            _inputReader.MovementKeyPressed += OnMovementKeyPressed;
            _timer.Finished += OnTimeOut;
            _levelProgress.QuotaCompleted += OnQuotaCompleted;
        }

        private void Start()
        {
            _musicPlayer.Play(_musicTrack);
        }

        private void OnDisable()
        {
            _inputReader.MovementKeyPressed -= OnMovementKeyPressed;
            _timer.Finished -= OnTimeOut;

            if (_levelProgress != null)
            {
                _levelProgress.QuotaCompleted -= OnQuotaCompleted;
            }
        }

        public void ExitToMenu()
        {
            StopGameplay();
            NavigateTo(SceneId.Menu).Forget();
        }

        private void StopGameplay()
        {
            if (_state != SessionState.Running)
            {
                return;
            }

            _gameplayReporter.ReportStop();
        }

        private void OnMovementKeyPressed()
        {
            if (TryTransitTo(SessionState.Running) == false)
            {
                return;
            }

            _pauser.RequestResume();
            _timer.StartCount();
            _gameplayReporter.ReportStart();
        }

        private void OnTimeOut()
        {
            if (TryTransitTo(SessionState.Finished) == false)
            {
                return;
            }

            FinishGame();
        }

        private void OnQuotaCompleted()
        {
            if (TryTransitTo(SessionState.Finished) == false)
            {
                return;
            }

            FinishGame();
        }

        private bool TryTransitTo(SessionState targetState)
        {
            if (_state == SessionState.Finished)
            {
                return false;
            }

            if (targetState <= _state)
            {
                return false;
            }

            _state = targetState;
            return true;
        }

        private void FinishGame()
        {
            _timer.Stop();
            _gameplayReporter.ReportStop();
            _pauser.RequestPause();

            FinishDelayAsync().Forget();
        }

        private async UniTaskVoid FinishDelayAsync()
        {
            try
            {
                if (_sessionEndDelay > 0f)
                {
                    await UniTask.Delay
                    (
                        (int)(_sessionEndDelay * 1000f),
                        DelayType.Realtime,
                        cancellationToken: this.GetCancellationTokenOnDestroy()
                    );
                }
            }
            catch (OperationCanceledException)
            {
                return;
            }

            NavigateTo(SceneId.Fill).Forget();
        }

        private async UniTaskVoid NavigateTo(SceneId targetSceneId)
        {
            if (_gameDirector.IsTransitioning == true)
            {
                return;
            }

            await _gameDirector.LoadAsync(targetSceneId);
        }
    }
}
