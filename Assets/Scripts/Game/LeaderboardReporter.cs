#if Leaderboard_yg
using UnityEngine;
using YG;

namespace Game
{
    public sealed class LeaderboardReporter : MonoBehaviour
    {
        [SerializeField] private string _leaderboardName;

        public void Report(int score)
        {
            YG2.NewLeaderboardScore(_leaderboardName, score);
        }
    }
}
#endif
