using System;

namespace Core
{
    public interface ILeaderboardService
    {
        bool IsAuthorized { get; }

        string PlayerId { get; }

        string PlayerName { get; }

        event Action<LeaderboardSnapshot> EntriesReceived;

        event Action EntriesFailed;

        void SetScore(string leaderboardName, int score);

        void RequestEntries(string leaderboardName, int topCount, int aroundCount, string photoSize);

        void OpenAuthDialog();
    }
}
