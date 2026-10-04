using Core;
using System;
using System.Collections.Generic;
using UnityEngine;
using Upgrades;
using VContainer;

namespace Game
{
    public sealed class PlayerProgress : MonoBehaviour
    {
        private static readonly string[] LegacySkinIds =
        {
            "Slime",
            "Pacman",
            "Tung",
            "Crown",
            "Phantom"
        };

        private ISavesAccess _saves;

        public event Action Ready;

        public bool IsReady => _saves.IsReady;

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

        public string SelectedSkinId
        {
            get
            {
                return _saves.SelectedSkinId;
            }
            set
            {
                _saves.SelectedSkinId = value;
            }
        }

        public List<string> OpenSkinIds => _saves.OpenSkinIds;

        public List<string> ShowcaseSkinIds => _saves.ShowcaseSkinIds;

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
            _saves.Ready += OnSavesReady;

            if (_saves.IsReady == true)
            {
                OnSavesReady();
            }
        }

        private void OnDestroy()
        {
            if (_saves != null)
            {
                _saves.Ready -= OnSavesReady;
            }
        }

        private void OnSavesReady()
        {
            MigrateLegacySkinSaves();

            Action ready = Ready;
            ready?.Invoke();
        }

        private void MigrateLegacySkinSaves()
        {
            if (string.IsNullOrEmpty(_saves.SelectedSkinId) == false)
            {
                return;
            }

            _saves.SelectedSkinId = ResolveLegacySkinId(_saves.LegacySelectedSkinIndex);

            List<int> legacyOpenSkins = _saves.LegacyOpenSkinIndices;

            for (int i = 0; i < legacyOpenSkins.Count; i++)
            {
                string legacyId = ResolveLegacySkinId(legacyOpenSkins[i]);

                if (_saves.OpenSkinIds.Contains(legacyId) == false)
                {
                    _saves.OpenSkinIds.Add(legacyId);
                }
            }

            _saves.Save();
        }

        private static string ResolveLegacySkinId(int legacyIndex)
        {
            if (legacyIndex < 0 || legacyIndex >= LegacySkinIds.Length)
            {
                throw new InvalidOperationException(
                    $"PlayerProgress: legacy skin index {legacyIndex} is outside the migration table " +
                    $"of {LegacySkinIds.Length} entries.");
            }

            return LegacySkinIds[legacyIndex];
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
