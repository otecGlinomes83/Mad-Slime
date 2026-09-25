using System;
using Scriptables;
using UnityEngine;
using YG;

namespace Game
{
    public sealed class LeaderboardReporter : MonoBehaviour
    {
        [SerializeField] private YandexConfig _config;

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

        public void Report(int score)
        {
            if (YG2.player.auth == false)
            {
                YG2.Message("Leaderboard: player is not authorized, score is not reported.");
                return;
            }

            YG2.SetLeaderboard(_config.LeaderboardName, score);
        }
    }
}
