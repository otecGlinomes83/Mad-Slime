using System;
using Scriptables;
using UnityEngine;

namespace Game
{
    public sealed class LeaderboardReporter : MonoBehaviour
    {
        [SerializeField] private YandexConfig _config;

        private YandexAdsBridge _bridge;

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
        }

        public void Setup(YandexAdsBridge bridge)
        {
            _bridge = bridge ?? throw new ArgumentNullException(nameof(bridge));
        }

        public void Report(int score, string playerExtra)
        {
            if (_bridge == null)
            {
                throw new InvalidOperationException(
                    $"{name}: Setup was not called. The bridge must be passed before the first Report.");
            }

            _bridge.SetLeaderboardScore(_config.LeaderboardName, score, playerExtra);
        }
    }
}
