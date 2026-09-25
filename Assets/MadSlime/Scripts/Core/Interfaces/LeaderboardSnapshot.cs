namespace Core
{
    public sealed class LeaderboardSnapshot
    {
        public string TechnoName;
        public bool HasEntries;
        public LeaderboardEntryData[] Players;
        public LeaderboardEntryData CurrentPlayer;
        public bool HasCurrentPlayer;
    }
}
