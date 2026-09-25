using System;
using Core;
using Player;
using System.Collections.Generic;
using Upgrades;
using YG;

namespace Adapters
{
    public sealed class Yg2SavesAccess : ISavesAccess
    {
        public bool IsReady => YG2.isSDKEnabled;

        public event Action Ready
        {
            add
            {
                YG2.onGetSDKData += value;
            }
            remove
            {
                YG2.onGetSDKData -= value;
            }
        }

        public int Balance
        {
            get
            {
                return YG2.saves.Balance;
            }
            set
            {
                YG2.saves.Balance = value;
            }
        }

        public int CurrentLevel
        {
            get
            {
                return YG2.saves.CurrentLevel;
            }
            set
            {
                YG2.saves.CurrentLevel = value;
            }
        }

        public int MaxLevel
        {
            get
            {
                return YG2.saves.MaxLevel;
            }
            set
            {
                YG2.saves.MaxLevel = value;
            }
        }

        public string Language
        {
            get
            {
                return YG2.saves.Language;
            }
            set
            {
                YG2.saves.Language = value;
            }
        }

        public float MusicVolume
        {
            get
            {
                return YG2.saves.musicVolume;
            }
            set
            {
                YG2.saves.musicVolume = value;
            }
        }

        public float SfxVolume
        {
            get
            {
                return YG2.saves.sfxVolume;
            }
            set
            {
                YG2.saves.sfxVolume = value;
            }
        }

        public PlayerSkins SelectedSkinType
        {
            get
            {
                return YG2.saves.SelectedSkinType;
            }
            set
            {
                YG2.saves.SelectedSkinType = value;
            }
        }

        public List<PlayerSkins> OpenSkins => YG2.saves._openSkins;

        public int SpeedLevel
        {
            get
            {
                return YG2.saves.SpeedLevel;
            }
            set
            {
                YG2.saves.SpeedLevel = value;
            }
        }

        public int AppetiteLevel
        {
            get
            {
                return YG2.saves.AppetiteLevel;
            }
            set
            {
                YG2.saves.AppetiteLevel = value;
            }
        }

        public int TasteLevel
        {
            get
            {
                return YG2.saves.TasteLevel;
            }
            set
            {
                YG2.saves.TasteLevel = value;
            }
        }

        public int MetabolismLevel
        {
            get
            {
                return YG2.saves.MetabolismLevel;
            }
            set
            {
                YG2.saves.MetabolismLevel = value;
            }
        }

        public List<PerkType> PurchasedPerks => YG2.saves.PurchasedPerks;

        public long LastFreeSpinUnixTime
        {
            get
            {
                return YG2.saves.LastFreeSpinUnixTime;
            }
            set
            {
                YG2.saves.LastFreeSpinUnixTime = value;
            }
        }

        public List<long> RouletteAdSpinTimes => YG2.saves.RouletteAdSpinTimes;

        public int SkinSpinCount
        {
            get
            {
                return YG2.saves.SkinSpinCount;
            }
            set
            {
                YG2.saves.SkinSpinCount = value;
            }
        }

        public void Save()
        {
            if (YG2.isSDKEnabled == false)
            {
                return;
            }

            YG2.SaveProgress();
        }
    }
}
