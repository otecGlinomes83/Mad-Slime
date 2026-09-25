using Player;
using System;
using System.Collections.Generic;
using UnityEngine;
using Upgrades;
using YG;

namespace Game
{
    public sealed class PlayerProgress : MonoBehaviour
    {
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

        public PlayerSkins SelectedSkin
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

        public int GetUpgradeLevel(UpgradeType type)
        {
            switch (type)
            {
                case UpgradeType.Speed:
                    return YG2.saves.SpeedLevel;

                case UpgradeType.Appetite:
                    return YG2.saves.AppetiteLevel;

                case UpgradeType.Taste:
                    return YG2.saves.TasteLevel;

                case UpgradeType.Metabolism:
                    return YG2.saves.MetabolismLevel;

                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(type),
                        type,
                        "PlayerProgress.GetUpgradeLevel received an unknown upgrade type.");
            }
        }

        public void SetUpgradeLevel(UpgradeType type, int level)
        {
            switch (type)
            {
                case UpgradeType.Speed:
                    YG2.saves.SpeedLevel = level;
                    break;

                case UpgradeType.Appetite:
                    YG2.saves.AppetiteLevel = level;
                    break;

                case UpgradeType.Taste:
                    YG2.saves.TasteLevel = level;
                    break;

                case UpgradeType.Metabolism:
                    YG2.saves.MetabolismLevel = level;
                    break;

                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(type),
                        type,
                        "PlayerProgress.SetUpgradeLevel received an unknown upgrade type.");
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
