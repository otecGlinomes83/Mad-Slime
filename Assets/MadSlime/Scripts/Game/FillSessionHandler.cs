using Audio;
using Core;
using Cysharp.Threading.Tasks;
using Scriptables;
using ShapeFill;
using System;
using UnityEngine;
using VContainer;

namespace Game
{
    public sealed class FillSessionHandler : MonoBehaviour
    {
        [SerializeField] private SfxClip _musicTrack;

        private FillConfig _config;
        private MusicPlayer _musicPlayer;
        private PlayerProgress _progress;
        private LevelConfigResolver _configResolver;
        private ShapeFillOrchestrator _fillOrchestrator;
        private GridBuilder _gridBuilder;
        private GameDirector _gameDirector;
        private Rewarder _rewarder;
        private Pauser _pauser;
        private AdScheduler _adScheduler;
        private LeaderboardReporter _leaderboardReporter;
        private IGameplayReporter _gameplayReporter;
        private bool _isGameplayStarted;

        public event Action<int> Win;
        public event Action<int> Failed;

        public bool CanRescueFill => _fillOrchestrator.CanRescue;

        [Inject]
        public void Construct(PlayerProgress progress, LevelConfigResolver configResolver, MusicPlayer musicPlayer,
            ShapeFillOrchestrator fillOrchestrator, GridBuilder gridBuilder, GameDirector gameDirector,
            Rewarder rewarder, Pauser pauser, AdScheduler adScheduler, LeaderboardReporter leaderboardReporter,
            IGameplayReporter gameplayReporter, FillConfig config)
        {
            _progress = progress;
            _configResolver = configResolver;
            _config = config;
            _musicPlayer = musicPlayer;
            _fillOrchestrator = fillOrchestrator;
            _gridBuilder = gridBuilder;
            _gameDirector = gameDirector;
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

            if (_musicPlayer == null)
            {
                throw new InvalidOperationException(
                    $"{name}: MusicPlayer was not injected. FillLifetimeScope must be the first object in the scene hierarchy.");
            }

            if (_musicTrack == null)
            {
                throw new InvalidOperationException(
                    $"{name}: Music track is not assigned. Drag a SfxClip asset into the _musicTrack field.");
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

            if (_gameDirector == null)
            {
                throw new InvalidOperationException(
                    $"{name}: GameDirector was not injected. Check that ProjectLifetimeScope registers GameDirector and FillLifetimeScope registers FillSessionHandler.");
            }

            if (_gridBuilder == null)
            {
                throw new InvalidOperationException(
                    $"{name}: GridBuilder was not injected. Check that FillLifetimeScope registers GridBuilder and FillSessionHandler.");
            }
        }

        private void OnEnable()
        {
            _fillOrchestrator.FillCompleted += OnFillCompleted;
            _rewarder.RewardGranted += OnRewardGranted;
        }

        private void Start()
        {
            _musicPlayer.Play(_musicTrack);
            ApplyTheme();
            _fillOrchestrator.StartFill();
            StartGameplay();
        }

        private void OnDisable()
        {
            _fillOrchestrator.FillCompleted -= OnFillCompleted;
            _rewarder.RewardGranted -= OnRewardGranted;
        }

        private void ApplyTheme()
        {
            if (_configResolver == null)
            {
                throw new InvalidOperationException(
                    $"{name}: dependencies were not injected. FillLifetimeScope must be the first object in the scene hierarchy.");
            }

            if (_gridBuilder == null)
            {
                throw new InvalidOperationException(
                    $"{name}: GridBuilder was not injected. Check that FillLifetimeScope registers GridBuilder and FillSessionHandler.");
            }

            LevelConfig config = _configResolver.GetConfigFor(_progress.CurrentLevel);

            if (config.Theme == null || config.Theme.FillShapeTexture == null)
            {
                Debug.LogWarning(
                    $"[Fill] LevelConfig '{config.name}' has no Theme or FillShapeTexture. GridBuilder keeps its scene-authored texture.");
                return;
            }

            _gridBuilder.SetShapeTexture(config.Theme.FillShapeTexture);

            Debug.Log($"[Fill] theme '{config.Theme.name}' applied, fill texture '{config.Theme.FillShapeTexture.name}'.");
        }

        public void LoadNextLevel()
        {
            _progress.CurrentLevel++;

            if (_progress.CurrentLevel > _progress.MaxLevel)
            {
                _progress.MaxLevel = _progress.CurrentLevel;
            }

            _progress.Save();
            _leaderboardReporter.Report(_progress.MaxLevel);

            NavigateToAfterStop(SceneId.Game);
        }

        public void RestartLevel()
        {
            NavigateToAfterStop(SceneId.Game);
        }

        public void ExitToMenuAfterWin()
        {
            _progress.CurrentLevel++;

            if (_progress.CurrentLevel > _progress.MaxLevel)
            {
                _progress.MaxLevel = _progress.CurrentLevel;
            }

            _progress.Save();
            _leaderboardReporter.Report(_progress.MaxLevel);

            NavigateToAfterStop(SceneId.Menu);
        }

        public void ExitToMenu()
        {
            _progress.Save();
            NavigateToAfterStop(SceneId.Menu);
        }

        public void RescueFill()
        {
            _fillOrchestrator.Rescue();
            StartGameplay();
        }

        private void NavigateToAfterStop(SceneId targetSceneId)
        {
            StopGameplay();
            _adScheduler.TryShowInterstitial();
            NavigateTo(targetSceneId).Forget();
        }

        private async UniTaskVoid NavigateTo(SceneId targetSceneId)
        {
            if (_gameDirector.IsTransitioning == true)
            {
                return;
            }

            await _gameDirector.LoadAsync(targetSceneId);
        }

        private void StartGameplay()
        {
            if (_isGameplayStarted == true)
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
            StopGameplay();

            if (percent >= 1f)
            {
                RewardWinDelayedAsync(percent).Forget();
            }
            else
            {
                _rewarder.RewardLose(percent);
            }
        }

        private async UniTaskVoid RewardWinDelayedAsync(float percent)
        {
            try
            {
                await UniTask.Delay((int)(_config.WinDelay * 1000f), cancellationToken: this.GetCancellationTokenOnDestroy());
            }
            catch (OperationCanceledException)
            {
                return;
            }

            _rewarder.RewardWin(percent);
        }

        private void OnRewardGranted(int amount, bool isWin)
        {
            if (isWin)
            {
                Win?.Invoke(amount);
            }
            else
            {
                Failed?.Invoke(amount);
            }
        }
    }
}
