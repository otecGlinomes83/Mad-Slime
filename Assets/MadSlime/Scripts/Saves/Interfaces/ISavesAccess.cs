using System;
using System.Collections.Generic;
using Upgrades;

namespace Core
{
    public interface ISavesAccess
    {
        bool IsReady { get; }

        event Action Ready;

        int Balance { get; set; }

        int CurrentLevel { get; set; }

        int MaxLevel { get; set; }

        string Language { get; set; }

        float MusicVolume { get; set; }

        float SfxVolume { get; set; }

        int LegacySelectedSkinIndex { get; }

        List<int> LegacyOpenSkinIndices { get; }

        string SelectedSkinId { get; set; }

        List<string> OpenSkinIds { get; }

        List<string> ShowcaseSkinIds { get; }

        int SpeedLevel { get; set; }

        int AppetiteLevel { get; set; }

        int TasteLevel { get; set; }

        int MetabolismLevel { get; set; }

        List<PerkType> PurchasedPerks { get; }

        long LastFreeSpinUnixTime { get; set; }

        List<long> RouletteAdSpinTimes { get; }

        int SkinSpinCount { get; set; }

        void Save();
    }
}
