using Audio;
using Core;
using Cysharp.Threading.Tasks;
using Saves;
using Scriptables;
using ShapeFill;
using System;
using System.Threading;
using UnityEngine;
using VContainer;

namespace Game
{
    public class FillSessionHandler : MonoBehaviour
    {
        [SerializeField] private Playlist _musicPlaylist;

        [Tooltip("Пауза перед показом окна победы после финала заливки: игрок видит конфетти и пунш формы (с).")]
        [SerializeField, Min(0f)] private float _winWindowDelay = 1f;

        private FillConfig _config;
        private IMusicPlayer _musicPlayer;
        private ILevelStorage _levelStorage;
        private LevelConfigResolver _configResolver;
        private ShapeFillOrchestrator _fillOrchestrator;
        private GridBuilder _gridBuilder;
        private SceneNavigator _sceneNavigator;
        private Rewarder _rewarder;
        private Pauser _pauser;
        private AdScheduler _adScheduler;
        private LeaderboardReporter _leaderboardReporter;
        private IGameplayReporter _gameplayReporter;
        private bool _isGameplayStarted;
        private bool _isPreparing;
        private bool _hasStarted;
        private bool _isFinished;
        private bool _isCompletionHandled;
        private bool _isRescueCommitted;
        private bool _isLevelProgressCommitted;
        private bool _canRescue;
        private FillResult _result;
        private Action<Action> _transaction;
        private CancellationTokenSource _sessionCancellation;
        private ISaveConfirmation _saveConfirmation;

        public event Action<int> Win;
        public event Action<int> Failed;

        public event Action Finished;

        public bool CanRescueFill => _canRescue;

        public bool IsFinished => _isFinished;

        [Inject]
        public void Construct(ILevelStorage levelStorage, LevelConfigResolver configResolver, IMusicPlayer musicPlayer,
            ShapeFillOrchestrator fillOrchestrator, GridBuilder gridBuilder, SceneNavigator sceneNavigator,
            Rewarder rewarder, Pauser pauser, AdScheduler adScheduler, LeaderboardReporter leaderboardReporter,
            IGameplayReporter gameplayReporter, FillConfig config, ISaveConfirmation saveConfirmation,
            Action<Action> transaction)
        {
            _levelStorage = levelStorage;
            _saveConfirmation = saveConfirmation;
            _transaction = transaction;
            _configResolver = configResolver;
            _config = config;
            _musicPlayer = musicPlayer;
            _fillOrchestrator = fillOrchestrator;
            _gridBuilder = gridBuilder;
            _sceneNavigator = sceneNavigator;
            _rewarder = rewarder;
            _pauser = pauser;
            _adScheduler = adScheduler;
            _leaderboardReporter = leaderboardReporter;
            _gameplayReporter = gameplayReporter;
        }

        private void Awake()
        {
            if (_config == null)
            {
                throw new InvalidOperationException(
                    $"{name}: FillConfig was not injected. Check that FillLifetimeScope has the FillConfig assigned.");
            }

            if (_musicPlaylist == null)
            {
                throw new InvalidOperationException(
                    $"{name}: Music playlist is not assigned. Drag a Playlist asset into the _musicPlaylist field.");
            }

            if (_adScheduler == null)
            {
                throw new InvalidOperationException(
                    $"{name}: AdScheduler was not injected. Check that FillLifetimeScope registers AdScheduler and FillSessionHandler.");
            }

            if (_leaderboardReporter == null)
            {
                throw new InvalidOperationException(
                    $"{name}: LeaderboardReporter was not injected. Check that FillLifetimeScope registers LeaderboardReporter and FillSessionHandler.");
            }

            if (_fillOrchestrator == null)
            {
                throw new InvalidOperationException(
                    $"{name}: ShapeFillOrchestrator was not injected. Check that FillLifetimeScope registers ShapeFillOrchestrator and FillSessionHandler.");
            }

            if (_rewarder == null)
            {
                throw new InvalidOperationException(
                    $"{name}: Rewarder was not injected. Check that FillLifetimeScope registers Rewarder and FillSessionHandler.");
            }

            if (_gameplayReporter == null)
            {
                throw new InvalidOperationException(
                    $"{name}: IGameplayReporter was not injected. Check that ProjectLifetimeScope registers the YG2 gameplay adapter.");
            }

            if (_sceneNavigator == null)
            {
                throw new InvalidOperationException(
                    $"{name}: SceneNavigator was not injected. Check that ProjectLifetimeScope registers SceneNavigator and FillLifetimeScope registers FillSessionHandler.");
            }

            if (_gridBuilder == null)
            {
                throw new InvalidOperationException(
                    $"{name}: GridBuilder was not injected. Check that FillLifetimeScope registers GridBuilder and FillSessionHandler.");
            }
        }

        private void OnEnable()
        {
            if (_isFinished == true)
            {
                return;
            }

            _sessionCancellation = CancellationTokenSource.CreateLinkedTokenSource(this.GetCancellationTokenOnDestroy());
            _fillOrchestrator.FillCompleted += OnFillCompleted;
        }

        private void OnDisable()
        {
            _sceneNavigator.UnregisterSession(StartSession);
            Finish();
            if (_fillOrchestrator != null)
            {
                _fillOrchestrator.FillCompleted -= OnFillCompleted;
            }
        }

        private void Start()
        {
            _sceneNavigator.RegisterSession(PrepareSessionAsync, StartSession, Finish, _sessionCancellation.Token);
        }

        private async UniTask PrepareSessionAsync(CancellationToken cancellationToken)
        {
            if (_isFinished == true || _isPreparing == true || _hasStarted == true)
            {
                throw new InvalidOperationException("Fill session cannot be prepared twice.");
            }

            _isPreparing = true;
            ApplyTheme();
            _fillOrchestrator.Prepare();
            _result = _fillOrchestrator.CalculateResult();
            _rewarder.ResetLevelResult();
            _transaction(CommitResult);

            if (_rewarder.IsWin == true)
            {
                _leaderboardReporter.Report();
            }

            await _saveConfirmation.ConfirmSavedAsync(cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
        }

        private void StartSession()
        {
            if (_hasStarted == true || _isFinished == true)
            {
                return;
            }

            _hasStarted = true;
            _isPreparing = false;
            _musicPlayer.Play(_musicPlaylist);
            _fillOrchestrator.Show(_result);
            StartGameplay();
        }

        private void CommitResult()
        {
            _rewarder.CommitLevelResult(_result.Percent);

            if (_rewarder.IsWin == true)
            {
                CommitLevelCompletion();
            }
        }

        public void LoadNextLevel()
        {
            NavigateToAfterStop(SceneId.Game);
        }

        public void RestartLevel()
        {
            NavigateToAfterStop(SceneId.Game);
        }

        public void ExitToMenuAfterWin()
        {
            NavigateToAfterStop(SceneId.Menu);
        }

        public void ExitToMenu()
        {
            NavigateToAfterStop(SceneId.Menu);
        }

        private void CommitLevelCompletion()
        {
            if (_isLevelProgressCommitted == true)
            {
                return;
            }

            _isLevelProgressCommitted = true;
            int nextLevel = _levelStorage.CurrentLevel + 1;
            int maxLevel = Mathf.Max(_levelStorage.MaxLevel, nextLevel);

            _levelStorage.SetLevelProgress(nextLevel, maxLevel);
        }

        public void RescueFill()
        {
            if (_isFinished == true || _canRescue == false || _isPreparing == true || _isRescueCommitted == true)
            {
                return;
            }

            _isPreparing = true;
            _canRescue = false;
            _isRescueCommitted = true;
            RescueFillAsync().Forget();
        }

        private async UniTaskVoid RescueFillAsync()
        {
            CancellationToken cancellationToken = _sessionCancellation.Token;

            try
            {
                _transaction(CommitRescue);
                _leaderboardReporter.Report();
                await _saveConfirmation.ConfirmSavedAsync(cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();
                _isCompletionHandled = false;
                _isPreparing = false;
                _fillOrchestrator.RescueShow();
                StartGameplay();
            }
            catch (OperationCanceledException)
            {
                return;
            }
        }

        private void CommitRescue()
        {
            _rewarder.CommitRescueTopUp();
            CommitLevelCompletion();
        }

        public void PauseByRequest()
        {
            if (_isFinished == false)
            {
                _pauser.RequestPause();
            }
        }

        public void ResumeByRequest()
        {
            _pauser.RequestResume();
        }

        private void NavigateToAfterStop(SceneId targetSceneId)
        {
            if (_isFinished == true || _sceneNavigator.IsTransitioning == true)
            {
                return;
            }

            Finish();
            _adScheduler.TryShowInterstitial();
            NavigateTo(targetSceneId).Forget();
        }

        private async UniTaskVoid NavigateTo(SceneId targetSceneId)
        {
            if (_sceneNavigator.IsTransitioning == true)
            {
                return;
            }

            if (targetSceneId == SceneId.Game)
            {
                await _sceneNavigator.LoadGameAsync();
                return;
            }

            await _sceneNavigator.LoadMenuAsync();
        }

        private void StartGameplay()
        {
            if (_isGameplayStarted == true || _isFinished == true || _isCompletionHandled == true)
            {
                return;
            }

            _isGameplayStarted = true;
            _gameplayReporter.ReportStart();
        }

        private void StopGameplay()
        {
            if (_isGameplayStarted == false)
            {
                return;
            }

            _isGameplayStarted = false;
            _gameplayReporter.ReportStop();
        }

        private void OnFillCompleted(float percent)
        {
            if (_isFinished == true || _isPreparing == true || _isCompletionHandled == true)
            {
                return;
            }

            _isCompletionHandled = true;
            StopGameplay();

            if (_rewarder.IsWin == true)
            {
                ShowWinWindowDelayed().Forget();
                return;
            }

            _canRescue = _fillOrchestrator.CanRescue;
            Failed?.Invoke(_rewarder.GrantedTotal);
        }

        private async UniTaskVoid ShowWinWindowDelayed()
        {
            try
            {
                if (_winWindowDelay > 0f)
                {
                    await UniTask.Delay((int)(_winWindowDelay * 1000f),
                        cancellationToken: _sessionCancellation.Token);
                }
            }
            catch (OperationCanceledException)
            {
                return;
            }

            if (_isFinished == false)
            {
                Win?.Invoke(_rewarder.GrantedTotal);
            }
        }

        private void Finish()
        {
            if (_isFinished == true)
            {
                return;
            }

            _isFinished = true;
            _canRescue = false;
            StopGameplay();
            _fillOrchestrator.Stop();

            if (_sessionCancellation != null)
            {
                _sessionCancellation.Cancel();
                _sessionCancellation.Dispose();
                _sessionCancellation = null;
            }

            Finished?.Invoke();
        }

        private void ApplyTheme()
        {
            LevelConfig config = _configResolver.GetConfigFor(_levelStorage.CurrentLevel);

            if (config.Theme == null || config.Theme.FillShapeTexture == null)
            {
                return;
            }

            _gridBuilder.SetShapeTexture(config.Theme.FillShapeTexture);
        }
    }
}
