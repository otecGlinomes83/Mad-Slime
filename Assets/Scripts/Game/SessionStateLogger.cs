using UnityEngine;
using UnityEngine.SceneManagement;
using VContainer;
using VContainer.Unity;

namespace Game
{
    public sealed class SessionStateLogger : IStartable
    {
        private readonly PlayerProgress _progress;
        private readonly LevelProgress _levelProgress;

        [Inject]
        public SessionStateLogger(PlayerProgress progress, LevelProgress levelProgress)
        {
            _progress = progress;
            _levelProgress = levelProgress;
        }

        void IStartable.Start()
        {
            LogState("session start");

            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            LogState($"scene '{scene.name}' loaded");
        }

        private void LogState(string reason)
        {
            Debug.Log(
                $"[Session] {reason} | Level={_progress.CurrentLevel} Balance={_progress.Balance} | Quota {_levelProgress.CollectedQuotaCount}/{_levelProgress.TotalQuotaTarget} Fill={_levelProgress.FillPercent:0.00}");
        }
    }
}
