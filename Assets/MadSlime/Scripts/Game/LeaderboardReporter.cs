using Core;
using Scriptables;
using System;
using UnityEngine;
using VContainer;

namespace Game
{
    public sealed class LeaderboardReporter : MonoBehaviour
    {
        [SerializeField] private YandexConfig _config;

        private ILeaderboardService _leaderboardService;

        [Inject]
        public void Construct(ILeaderboardService leaderboardService)
        {
            _leaderboardService = leaderboardService;
        }

        private void Awake()
        {
            if (_config == null)
            {
                throw new InvalidOperationException(
                    $"{name}: YandexConfig is not assigned. Drag the YandexConfig asset into the _config field.");
            }

            if (string.IsNullOrEmpty(_config.LeaderboardName))
            {
                throw new InvalidOperationException(
                    $"{name}: LeaderboardName is empty. Fill it in the YandexConfig asset (id of the leaderboard from the Yandex console).");
            }

            if (_leaderboardService == null)
            {
                throw new InvalidOperationException(
                    $"{name}: ILeaderboardService was not injected. Check that ProjectLifetimeScope registers the YG2 leaderboard adapter.");
            }
        }

        public void Report(int score)
        {
            if (_leaderboardService.IsAuthorized == false)
            {
                Debug.Log("Leaderboard: player is not authorized, score is not reported.");
                return;
            }

            _leaderboardService.SetScore(_config.LeaderboardName, score);
        }
    }
}
