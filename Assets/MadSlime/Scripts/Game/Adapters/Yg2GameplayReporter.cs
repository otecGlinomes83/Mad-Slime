using Core;
using YG;

namespace Adapters
{
    public sealed class Yg2GameplayReporter : IGameplayReporter
    {
        public void ReportStart()
        {
            YG2.GameplayStart();
        }

        public void ReportStop()
        {
            YG2.GameplayStop();
        }
    }
}
