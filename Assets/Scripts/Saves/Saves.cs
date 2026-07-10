using Player;
using System.Collections.Generic;

namespace YG
{
    public partial class SavesYGQuota
    {
        public int QuotaCount;
        public int DefaultCount;
        public int TargetQuotaCount;

        public int CurrentLevel = 1;

        public float musicVolume = 0.5f;
        public float sfxVolume = 0.35f;

        public int Balance= 250;

        public PlayerSkins _selectedSkin = PlayerSkins.Slime;
        public List<PlayerSkins> _openSkins = new List<PlayerSkins>() { PlayerSkins.Slime };
    }
}