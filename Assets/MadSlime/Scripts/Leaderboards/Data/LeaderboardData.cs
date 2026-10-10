namespace Core
{
    public class LeaderboardData
    {
        private string _technoName;
        private LeaderboardEntryData[] _players;
        private LeaderboardEntryData? _currentPlayer;

        public string TechnoName => _technoName;

        public LeaderboardEntryData[] Players => _players;

        public LeaderboardEntryData? CurrentPlayer => _currentPlayer;

        public LeaderboardData(string technoName, LeaderboardEntryData[] players, LeaderboardEntryData? currentPlayer)
        {
            _technoName = technoName;
            _players = players;
            _currentPlayer = currentPlayer;
        }
    }
}
