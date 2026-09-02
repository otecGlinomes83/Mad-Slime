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

#if Leaderboard_yg
        [SerializeField] private LeaderboardReporter _leaderboardReporter;
#endif

        private PlayerProgress _progress;
        private LevelConfigResolver _configResolver;

        public event Action<int> Win;
        public event Action<int> Failed;

        [Inject]
        public void Construct(PlayerProgress progress, LevelConfigResolver configResolver)
        {
            _progress = progress;
            _configResolver = configResolver;
        }

        private void Start()
        {
            ApplyTheme();

            _fillOrchestrator.FillCompleted += OnFillCompleted;
            _fillOrchestrator.StartFill();
            _rewarder.RewardGranted += OnRewardGranted;
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
            _progress.Save();

#if Leaderboard_yg
            if (_leaderboardReporter != null)
            {
                _leaderboardReporter.Report(_progress.CurrentLevel);
            }
#endif

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
                _rewarder.RewardWin(percent);
            }
            else
            {
                _rewarder.RewardLose(percent);
            }
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
