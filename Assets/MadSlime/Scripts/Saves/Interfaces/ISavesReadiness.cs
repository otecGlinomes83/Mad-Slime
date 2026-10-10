using System;

namespace Saves
{
    public interface ISavesReadiness
    {
        bool IsReady { get; }

        event Action Ready;
    }
}
