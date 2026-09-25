using System;
using Core;
using System.Collections.Generic;
using YG;
using YG.Utils.LB;

namespace Adapters
{
    public sealed class Yg2LeaderboardService : ILeaderboardService
    {
        public bool IsAuthorized => YG2.player.auth;

        public string PlayerId => YG2.player.id;

        public string PlayerName => YG2.player.name;

        public event Action<LeaderboardSnapshot> EntriesReceived;

        public Yg2LeaderboardService()
        {
            YG2.onGetLeaderboard += OnLeaderboardReceived;
        }

        public void SetScore(string leaderboardName, int score)
        {
            YG2.SetLeaderboard(leaderboardName, score);
        }

        public void RequestEntries(string leaderboardName, int topCount, int aroundCount, string photoSize)
        {
            YG2.GetLeaderboard(leaderboardName, topCount, aroundCount, photoSize);
        }

        public void OpenAuthDialog()
        {
            YG2.OpenAuthDialog();
        }

        private void OnLeaderboardReceived(LBData data)
        {
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
