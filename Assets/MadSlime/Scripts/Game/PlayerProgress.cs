using Core;
using System;
using System.Collections.Generic;
using Player;
using UnityEngine;
using Upgrades;
using VContainer;

namespace Game
{
    public sealed class PlayerProgress : MonoBehaviour
    {
        private ISavesAccess _saves;

        public bool IsReady => _saves.IsReady;

        public event Action Ready
        {
            add
            {
                _saves.Ready += value;
            }
            remove
            {
                _saves.Ready -= value;
            }
        }

        public int CurrentLevel
        {
            get
            {
                return _saves.CurrentLevel;
            }
            set
            {
                _saves.CurrentLevel = value;
            }
        }

        public int MaxLevel
        {
            get
            {
                return _saves.MaxLevel;
            }
            set
            {
                _saves.MaxLevel = value;
            }
        }

        public string Language
        {
            get
            {
                return _saves.Language;
            }
            set
            {
                _saves.Language = value;
            }
        }

        public int Balance
        {
            get
            {
                return _saves.Balance;
            }
            set
            {
                _saves.Balance = value;
            }
        }

        public PlayerSkins SelectedSkin
        {
            get
            {
                return _saves.SelectedSkinType;
            }
            set
            {
                _saves.SelectedSkinType = value;
            }
        }

        public List<PlayerSkins> OpenSkins => _saves.OpenSkins;

        public float MusicVolume
        {
            get
            {
                return _saves.MusicVolume;
            }
            set
            {
                _saves.MusicVolume = value;
            }
        }

        public float SfxVolume
        {
            get
            {
                return _saves.SfxVolume;
            }
            set
            {
                _saves.SfxVolume = value;
            }
        }

        public List<PerkType> PurchasedPerks => _saves.PurchasedPerks;

        public long LastFreeSpinUnixTime
        {
            get
            {
                return _saves.LastFreeSpinUnixTime;
            }
            set
            {
                _saves.LastFreeSpinUnixTime = value;
            }
        }

        public List<long> RouletteAdSpinTimes => _saves.RouletteAdSpinTimes;

        public int SkinSpinCount
        {
            get
            {
                return _saves.SkinSpinCount;
            }
            set
            {
                _saves.SkinSpinCount = value;
            }
        }

        [Inject]
        public void Construct(ISavesAccess saves)
        {
            _saves = saves;
        }

        public int GetUpgradeLevel(UpgradeType type)
        {
            switch (type)
            {
                case UpgradeType.Speed:
                    return _saves.SpeedLevel;

                case UpgradeType.Appetite:
                    return _saves.AppetiteLevel;

                case UpgradeType.Taste:
                    return _saves.TasteLevel;

                case UpgradeType.Metabolism:
                    return _saves.MetabolismLevel;

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
                    _saves.SpeedLevel = level;
                    break;

                case UpgradeType.Appetite:
                    _saves.AppetiteLevel = level;
                    break;

                case UpgradeType.Taste:
                    _saves.TasteLevel = level;
                    break;

                case UpgradeType.Metabolism:
                    _saves.MetabolismLevel = level;
                    break;

                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(type),
                        type,
                        "PlayerProgress.SetUpgradeLevel received an unknown upgrade type.");
            }
        }

        public void Save()
        {
            _saves.Save();
        }
    }
}
