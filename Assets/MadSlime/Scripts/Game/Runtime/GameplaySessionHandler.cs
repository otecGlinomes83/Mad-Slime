using Audio;
using Collectables;
using Core;
using Cysharp.Threading.Tasks;
using Player;
using PlayerInput;
using Quota;
using Saves;
using Scriptables;
using System;
using System.Threading;
using Movement;
using UnityEngine;
using VContainer;

namespace Game
{
    public class GameplaySessionHandler : MonoBehaviour
    {
        private enum SessionState
        {
            Preparing,
            WaitingForStart,
            Running,
            Finished
        }

        [SerializeField] private Playlist _musicPlaylist;

        [Tooltip("Пауза перед переходом в Fill после конца сессии: игрок видит, что игра закончилась.")]
        [SerializeField, Min(0f)] private float _sessionEndDelay = 1f;

        [Tooltip("Сколько последних секунд уровня считается финальным отсчётом: на нём включаются адреналин, виньетка и тик.")]
        [SerializeField, Min(0.1f)] private float _finalCountdownSeconds = 20f;

        private LevelConfigResolver _configResolver;
        private LevelGenerator _levelGenerator;
        private ItemCollector _itemCollector;
        private IMusicPlayer _musicPlayer;
        private ILevelStorage _levelStorage;
        private QuotaCounter _quotaCounter;
        private SceneNavigator _sceneNavigator;
        private Timer _timer;
        private PlayerInputReader _inputReader;
        private Pauser _pauser;
        private IGameplayReporter _gameplayReporter;
        private AdrenalineBoost _adrenalineBoost;
        private FinalCountdownVignette _finalCountdownVignette;
        private TimerTickSound _timerTickSound;

        private Movement.Movement _movement;
        private PlayerTier _playerTier;
        private PlayerScaler _playerScaler;
        private SmellAbilityActivator _smellAbility;

        private ItemDetector _itemDetector;
        private AttractableDetector _attractableDetector;
        private CloseItemDetector _closeDetector;
        private CrawlAnimator _crawlAnimator;
        private GrowthAnimator _growthAnimator;
        private CancellationTokenSource _sessionSource;
        private CancellationTokenSource _finishDelaySource;
        private SessionState _state = SessionState.Preparing;
        private bool _isFinalCountdownStarted;

        public event Action SceneSessionPrepared;

        public event Action SceneSessionStarted;

        public event Action GameplayStarted;

        public event Action SessionFinished;

        [Inject]
        public void Construct(LevelConfigResolver configResolver, ILevelStorage levelStorage, QuotaCounter quotaCounter,
            LevelGenerator levelGenerator, ItemCollector itemCollector, IMusicPlayer musicPlayer,
            SceneNavigator sceneNavigator, Timer timer, PlayerInputReader inputReader,
            Pauser pauser, IGameplayReporter gameplayReporter, AdrenalineBoost adrenalineBoost,
            FinalCountdownVignette finalCountdownVignette, TimerTickSound timerTickSound,
            Movement.Movement movement, PlayerTier playerTier, PlayerScaler playerScaler, SmellAbilityActivator smellAbility,
            ItemDetector itemDetector, AttractableDetector attractableDetector, CloseItemDetector closeDetector,
            CrawlAnimator crawlAnimator, GrowthAnimator growthAnimator)
        {
            _configResolver = configResolver;
            _levelGenerator = levelGenerator;
            _itemCollector = itemCollector;
            _levelStorage = levelStorage;
            _quotaCounter = quotaCounter;
            _musicPlayer = musicPlayer;
            _sceneNavigator = sceneNavigator;
            _timer = timer;
            _inputReader = inputReader;
            _pauser = pauser;
            _gameplayReporter = gameplayReporter;
            _adrenalineBoost = adrenalineBoost;
            _finalCountdownVignette = finalCountdownVignette;
            _timerTickSound = timerTickSound;
            _movement = movement;
            _playerTier = playerTier;
            _playerScaler = playerScaler;
            _smellAbility = smellAbility;
            _itemDetector = itemDetector;
            _attractableDetector = attractableDetector;
            _closeDetector = closeDetector;
            _crawlAnimator = crawlAnimator;
            _growthAnimator = growthAnimator;
        }

        private void Awake()
        {
            if (_configResolver == null)
            {
                throw new InvalidOperationException(
                    $"{name}: dependencies were not injected. GameLifetimeScope must be the first object in the scene hierarchy.");
            }

            if (_musicPlaylist == null)
            {
                throw new InvalidOperationException(
                    $"{name}: Music playlist is not assigned. Drag a Playlist asset into the _musicPlaylist field.");
            }

            if (_levelGenerator == null)
            {
                throw new InvalidOperationException(
                    $"{name}: LevelGenerator was not injected. Check that GameLifetimeScope registers LevelGenerator and GameplaySessionHandler.");
            }

            if (_itemCollector == null)
            {
                throw new InvalidOperationException(
                    $"{name}: ItemCollector was not injected. Check that GameLifetimeScope registers ItemCollector and GameplaySessionHandler.");
            }

            if (_sceneNavigator == null)
            {
                throw new InvalidOperationException(
                    $"{name}: SceneNavigator was not injected. Check that ProjectLifetimeScope registers SceneNavigator.");
            }

            if (_gameplayReporter == null)
            {
                throw new InvalidOperationException(
                    $"{name}: IGameplayReporter was not injected. Check that ProjectLifetimeScope registers the YG2 gameplay adapter.");
            }

            if (_adrenalineBoost == null)
            {
                throw new InvalidOperationException(
                    $"{name}: AdrenalineBoost was not injected. Check that GameLifetimeScope registers AdrenalineBoost and GameplaySessionHandler.");
            }

            if (_finalCountdownVignette == null)
            {
                throw new InvalidOperationException(
                    $"{name}: FinalCountdownVignette was not injected. Check that GameLifetimeScope registers FinalCountdownVignette and GameplaySessionHandler.");
            }

            if (_timerTickSound == null)
            {
                throw new InvalidOperationException(
                    $"{name}: TimerTickSound was not injected. Check that GameLifetimeScope registers TimerTickSound and GameplaySessionHandler.");
            }

        }

        private void OnEnable()
        {
            _sessionSource = CancellationTokenSource.CreateLinkedTokenSource(this.GetCancellationTokenOnDestroy());
            if (_quotaCounter == null)
            {
                throw new InvalidOperationException(
                    $"{name}: QuotaCounter was not injected. Check that ProjectLifetimeScope registers QuotaCounter and GameplaySessionHandler.");
            }

            _inputReader.MovementKeyPressed += OnMovementKeyPressed;
            _timer.Ticked += OnTimerTicked;
            _timer.Finished += OnTimeOut;
            _pauser.StateChanged += ApplyPause;
            _quotaCounter.QuotaCompleted += OnQuotaCompleted;

        }

        private void OnDisable()
        {
            FinishSession();
            _sceneNavigator.UnregisterSession(RunSession);
            _sessionSource.Dispose();
            _sessionSource = null;
            _inputReader.MovementKeyPressed -= OnMovementKeyPressed;
            _timer.Ticked -= OnTimerTicked;
            _timer.Finished -= OnTimeOut;
            _pauser.StateChanged -= ApplyPause;

            _quotaCounter.QuotaCompleted -= OnQuotaCompleted;
            _quotaCounter.Detach();
        }

        public void ExitToMenu()
        {
            FinishSession();
            NavigateToMenu().Forget();
        }

        public void PauseByRequest()
        {
            _pauser.RequestPause();
        }

        public void ResumeByRequest()
        {
            _pauser.RequestResume();
        }

        private void Start()
        {
            _sceneNavigator.RegisterSession(PrepareSessionAsync, RunSession, FinishSession, _sessionSource.Token);
        }

        private UniTask PrepareSessionAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _playerTier.Initialize();
            _playerScaler.ApplyInitialScale();
            _movement.Initialize();
            _smellAbility.Apply();
            DisableFinalCountdownEffects();

            LevelConfig config = _configResolver.GetConfigFor(_levelStorage.CurrentLevel);

            _timer.Setup(config.TimerDuration);
            _levelGenerator.Generate();
            _quotaCounter.ResetSession();
            _state = SessionState.WaitingForStart;
            _isFinalCountdownStarted = false;
            _pauser.RequestPause();
            ApplyPause();
            SceneSessionPrepared?.Invoke();
            return UniTask.CompletedTask;
        }

        private void RunSession()
        {
            _musicPlayer.Play(_musicPlaylist);
            _quotaCounter.Attach(_itemCollector);
            ApplyPause();

            Action sceneSessionStarted = SceneSessionStarted;
            sceneSessionStarted?.Invoke();
        }


        private void ApplyPause()
        {
            bool isRunning = _state == SessionState.Running && _pauser.IsPaused == false;
            _itemDetector.enabled = isRunning;
            _attractableDetector.enabled = isRunning;
            _closeDetector.enabled = isRunning;
            _crawlAnimator.enabled = isRunning;
            _smellAbility.SetPaused(isRunning == false);

            if (isRunning == false)
            {
                _movement.DisableControl();
                _itemCollector.PauseCollecting();
                return;
            }

            _movement.EnableControl();
            _itemCollector.StartCollecting();
        }

        private void FinishSession()
        {
            bool wasRunning = _state == SessionState.Running;
            bool wasFinished = _state == SessionState.Finished;
            _state = SessionState.Finished;
            _sessionSource?.Cancel();
            _finishDelaySource?.Cancel();
            _finishDelaySource?.Dispose();
            _finishDelaySource = null;
            _timer.Stop();
            _itemCollector.StopCollecting();
            _quotaCounter.Detach();
            _growthAnimator.Stop();
            ApplyPause();
            DisableFinalCountdownEffects();

            if (wasRunning)
            {
                _gameplayReporter.ReportStop();
            }

            if (wasFinished == false)
            {
                SessionFinished?.Invoke();
            }
        }

        private void OnMovementKeyPressed()
        {
            if (_state != SessionState.WaitingForStart || TryTransitTo(SessionState.Running) == false)
            {
                return;
            }

            Action gameplayStarted = GameplayStarted;
            gameplayStarted?.Invoke();

            _pauser.RequestResume();
            _timer.StartCount();
            _gameplayReporter.ReportStart();
        }

        private void OnTimerTicked(float remainingSeconds)
        {
            if (_state != SessionState.Running)
            {
                return;
            }

            if (_isFinalCountdownStarted == true)
            {
                return;
            }

            if (remainingSeconds > _finalCountdownSeconds)
            {
                return;
            }

            _isFinalCountdownStarted = true;

            _adrenalineBoost.Enable();
            _finalCountdownVignette.StartPulse();
            _timerTickSound.StartTicking();
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
            _itemCollector.StopCollecting();
            _timer.Stop();
            _growthAnimator.Stop();
            _gameplayReporter.ReportStop();
            _pauser.RequestPause();
            ApplyPause();
            DisableFinalCountdownEffects();
            SessionFinished?.Invoke();
            _finishDelaySource = CancellationTokenSource.CreateLinkedTokenSource(this.GetCancellationTokenOnDestroy());
            FinishDelayAsync(_finishDelaySource.Token).Forget();
        }

        private void DisableFinalCountdownEffects()
        {
            _adrenalineBoost.Disable();
            _finalCountdownVignette.StopPulse();
            _timerTickSound.StopTicking();
        }

        private async UniTaskVoid FinishDelayAsync(CancellationToken cancellationToken)
        {
            try
            {
                if (_sessionEndDelay > 0f)
                {
                    await UniTask.Delay
                    (
                        (int)(_sessionEndDelay * 1000f),
                        DelayType.Realtime,
                        cancellationToken: cancellationToken
                    );
                }
            }
            catch (OperationCanceledException)
            {
                return;
            }

            NavigateToFill().Forget();
        }

        private async UniTaskVoid NavigateToMenu()
        {
            if (_sceneNavigator.IsTransitioning == true)
            {
                return;
            }

            _pauser.ResetToPlay();

            await _sceneNavigator.LoadMenuAsync();
        }

        private async UniTaskVoid NavigateToFill()
        {
            if (_sceneNavigator.IsTransitioning == true)
            {
                return;
            }

            _pauser.ResetToPlay();

            await _sceneNavigator.LoadFillAsync();
        }
    }
}
