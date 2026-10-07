using System;
using Core;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using YG;
using YG.Utils.LB;

namespace Adapters
{
    public sealed class Yg2LeaderboardService : ILeaderboardService, IDisposable
    {
        private const float EntriesTimeoutSeconds = 8f;

        public bool IsAuthorized => YG2.player.auth;

        public string PlayerId => YG2.player.id;

        public string PlayerName => YG2.player.name;

        public event Action<LeaderboardSnapshot> EntriesReceived;

        public event Action EntriesFailed;

        private CancellationTokenSource _entriesWatchSource;

        public Yg2LeaderboardService()
        {
            YG2.onGetLeaderboard += OnLeaderboardReceived;
        }

        public void Dispose()
        {
            YG2.onGetLeaderboard -= OnLeaderboardReceived;
            CancelEntriesWatch();
        }

        public void SetScore(string leaderboardName, int score)
        {
            YG2.SetLeaderboard(leaderboardName, score);
        }

        public void RequestEntries(string leaderboardName, int topCount, int aroundCount, string photoSize)
        {
            CancelEntriesWatch();

            _entriesWatchSource = new CancellationTokenSource();
            WatchEntriesAsync(_entriesWatchSource).Forget();

            YG2.GetLeaderboard(leaderboardName, topCount, aroundCount, photoSize);
        }

        public void OpenAuthDialog()
        {
            YG2.OpenAuthDialog();
        }

        private async UniTaskVoid WatchEntriesAsync(CancellationTokenSource entriesWatchSource)
        {
            CancellationToken cancellationToken = entriesWatchSource.Token;

            try
            {
                await UniTask.Delay(TimeSpan.FromSeconds(EntriesTimeoutSeconds), true, PlayerLoopTiming.Update,
                    cancellationToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            if (entriesWatchSource.IsCancellationRequested == false)
            {
                EntriesFailed?.Invoke();
            }
        }

        private void CancelEntriesWatch()
        {
            if (_entriesWatchSource == null)
            {
                return;
            }

            _entriesWatchSource.Cancel();
            _entriesWatchSource.Dispose();
            _entriesWatchSource = null;
        }

        private void OnLeaderboardReceived(LBData data)
        {
            CancelEntriesWatch();

            LeaderboardEntryData[] players = MapPlayers(data.players);

            LeaderboardSnapshot snapshot = new LeaderboardSnapshot
            {
                TechnoName = data.technoName,
                HasEntries = data.entries != InfoYG.NO_DATA,
                Players = players,
                CurrentPlayer = MapCurrentPlayer(data.currentPlayer),
                HasCurrentPlayer = data.currentPlayer != null,
            };

            EntriesReceived?.Invoke(snapshot);
        }

        private LeaderboardEntryData[] MapPlayers(LBPlayerData[] sourcePlayers)
        {
            if (sourcePlayers == null)
            {
                return new LeaderboardEntryData[0];
            }

            List<LeaderboardEntryData> entries = new List<LeaderboardEntryData>(sourcePlayers.Length);

            for (int playerIndex = 0; playerIndex < sourcePlayers.Length; playerIndex++)
            {
                entries.Add(MapEntry(sourcePlayers[playerIndex]));
            }

            return entries.ToArray();
        }

        private LeaderboardEntryData MapEntry(LBPlayerData sourcePlayer)
        {
            return new LeaderboardEntryData
            {
                Id = sourcePlayer.uniqueID,
                Name = LBMethods.AnonymousName(sourcePlayer.name),
                Rank = sourcePlayer.rank,
                Score = sourcePlayer.score,
            };
        }

        private LeaderboardEntryData MapCurrentPlayer(LBCurrentPlayerData sourcePlayer)
        {
            if (sourcePlayer == null)
            {
                return new LeaderboardEntryData();
            }

            return new LeaderboardEntryData
            {
                Id = string.Empty,
                Name = string.Empty,
                Rank = sourcePlayer.rank,
                Score = sourcePlayer.score,
            };
        }
    }
}