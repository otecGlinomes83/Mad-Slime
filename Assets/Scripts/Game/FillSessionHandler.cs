using Audio;
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
        [SerializeField] private ShapeFillOrchestrator _fillOrchestrator;
        [SerializeField] private GridBuilder _gridBuilder;
        [SerializeField] private LevelTransitor _levelTransitor;
        [SerializeField] private Rewarder _rewarder;
        [SerializeField] private Pauser _pauser;
        [SerializeField] private AdScheduler _adScheduler;
        [SerializeField] private LeaderboardReporter _leaderboardReporter;
        [SerializeField, Min(0f)] private float _winDelay = 1.3f;
        [SerializeField] private SfxClip _musicTrack;

        private MusicPlayer _musicPlayer;
        private PlayerProgress _progress;
        private LevelConfigResolver _configResolver;

        public event Action<int> Win;
        public event Action<int> Failed;

        [Inject]
        public void Construct(PlayerProgress progress, LevelConfigResolver configResolver, MusicPlayer musicPlayer)
        {
            _progress = progress;
            _configResolver = configResolver;
            _musicPlayer = musicPlayer;
        }

        private void Awake()
        {
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
                    $"{name}: AdScheduler is not assigned. Drag an AdScheduler component into the _adScheduler field.");
            }

            if (_leaderboardReporter == null)
            {
                throw new InvalidOperationException(
                    $"{name}: LeaderboardReporter is not assigned. Drag a LeaderboardReporter component into the _leaderboardReporter field.");
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
                    $"{name}: GridBuilder is not assigned. Drag the GridBuilder component into the _gridBuilder field.");
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

            _adScheduler.ShowInterstitialIfNeeded(_progress.CurrentLevel);
            _levelTransitor.LoadGame();
        }

        public void RestartLevel()
        {
            _levelTransitor.LoadGame();
        }

        private void OnFillCompleted(float percent)
        {
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
                await UniTask.Delay((int)(_winDelay * 1000f), cancellationToken: this.GetCancellationTokenOnDestroy());
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
