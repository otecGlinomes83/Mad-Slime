using System.Collections.Generic;
using Upgrades;

namespace YG
{
    public partial class SavesYG
    {
        public int CurrentLevel = 1;
        public int MaxLevel = 1;
        public string Language = "";

        public float musicVolume = 0.5f;
        public float sfxVolume = 0.35f;

        public int Balance = 250;

        public int SelectedSkinType;
        public List<int> _openSkins = new List<int>() { 0 };

        public string SelectedSkinId = "";
        public List<string> _openSkinIds = new List<string>();
        public List<string> _showcaseSkinIds = new List<string>();

        public int SpeedLevel;
        public int AppetiteLevel;
        public int TasteLevel;
        public int MetabolismLevel;

        public List<PerkType> PurchasedPerks = new List<PerkType>();

        public long LastFreeSpinUnixTime;
        public List<long> RouletteAdSpinTimes = new List<long>();
        public int SkinSpinCount;
    }
}
