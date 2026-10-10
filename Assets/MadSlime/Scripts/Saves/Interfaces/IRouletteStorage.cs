namespace Saves
{
    public interface IRouletteStorage
    {
        long LastFreeSpinUnixTime { get; }

        void SetLastFreeSpinUnixTime(long unixTime);

        int CountAdSpinsSince(long windowStartUnixTime);

        void PruneAdSpinsBefore(long cutoffUnixTime);

        void RegisterAdSpin(long unixTime);

        int SkinSpinCount { get; }

        void SetSkinSpinCount(int count);
    }
}
