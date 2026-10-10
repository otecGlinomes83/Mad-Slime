using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using Adapters;
using UnityEngine;
using Upgrades;
using YG;

namespace Saves
{
    public class Saver : ISavesReadiness, IBalanceStorage, ILevelStorage, IAudioStorage, IUpgradesStorage,
        ISkinStorage, IRouletteStorage, ILanguageStorage, ICollectedItemsStorage, ISaveConfirmation
    {
        private const float MinVolume = 0f;
        private const float MaxVolume = 1f;

        private Yg2SavesAccess _access;

        public event Action Ready
        {
            add
            {
                _access.Ready += value;
            }
            remove
            {
                _access.Ready -= value;
            }
        }

        public bool IsReady => _access.IsReady;

        public Saver(Yg2SavesAccess access)
        {
            if (access == null)
            {
                throw new ArgumentNullException(nameof(access),
                    "Saver requires a Yg2SavesAccess instance.");
            }

            _access = access;
        }

        public UniTask ConfirmSavedAsync(CancellationToken cancellationToken)
        {
            return _access.ConfirmSavedAsync(cancellationToken);
        }

        public int CollectedItemsCount => LoadData().CollectedItemsCount;

        public void RegisterCollectedItem()
        {
            SavesYG data = LoadData();
            data.CollectedItemsCount = checked(data.CollectedItemsCount + 1);
            _isCollectedCountDirty = true;
        }

        private bool _isCollectedCountDirty;

        public void CommitCollectedItems()
        {
            if (_isCollectedCountDirty == false)
            {
                return;
            }

            SaveChanges();
            _isCollectedCountDirty = false;
        }

        public int Balance => LoadData().Balance;

        public int CurrentLevel => LoadData().CurrentLevel;

        public int MaxLevel => LoadData().MaxLevel;

        public float MusicVolume => LoadData().musicVolume;

        public float SfxVolume => LoadData().sfxVolume;

        public string SelectedSkinId => LoadData().SelectedSkinId;

        public long LastFreeSpinUnixTime => LoadData().LastFreeSpinUnixTime;

        public int SkinSpinCount => LoadData().SkinSpinCount;

        public string Language => LoadData().Language;

        public void SetBalance(int balance)
        {
            if (balance < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(balance),
                    balance,
                    "Saver.SetBalance requires a non-negative balance.");
            }

            SavesYG data = LoadData();
            data.Balance = balance;
            SaveChanges();
        }

        public void SetLevelProgress(int currentLevel, int maxLevel)
        {
            if (currentLevel <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(currentLevel),
                    currentLevel,
                    "Saver.SetLevelProgress requires a positive current level.");
            }

            if (maxLevel < currentLevel)
            {
                throw new ArgumentOutOfRangeException(nameof(maxLevel),
                    maxLevel,
                    "Saver.SetLevelProgress requires maxLevel to be greater or equal to currentLevel.");
            }

            SavesYG data = LoadData();
            data.CurrentLevel = currentLevel;
            data.MaxLevel = maxLevel;
            SaveChanges();
        }

        public void SetVolumes(float musicVolume, float sfxVolume)
        {
            SavesYG data = LoadData();
            data.musicVolume = Mathf.Clamp01(musicVolume);
            data.sfxVolume = Mathf.Clamp01(sfxVolume);
            SaveChanges();
        }

        public int GetUpgradeLevel(UpgradeType type)
        {
            SavesYG data = LoadData();

            switch (type)
            {
                case UpgradeType.Speed:
                    return data.SpeedLevel;

                case UpgradeType.Appetite:
                    return data.AppetiteLevel;

                case UpgradeType.Taste:
                    return data.TasteLevel;

                case UpgradeType.Metabolism:
                    return data.MetabolismLevel;

                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(type),
                        type,
                        "Saver.GetUpgradeLevel received an unknown upgrade type.");
            }
        }

        public void SetUpgradeLevel(UpgradeType type, int level)
        {
            if (level < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(level),
                    level,
                    "Saver.SetUpgradeLevel requires a non-negative level.");
            }

            SavesYG data = LoadData();

            switch (type)
            {
                case UpgradeType.Speed:
                    data.SpeedLevel = level;
                    break;

                case UpgradeType.Appetite:
                    data.AppetiteLevel = level;
                    break;

                case UpgradeType.Taste:
                    data.TasteLevel = level;
                    break;

                case UpgradeType.Metabolism:
                    data.MetabolismLevel = level;
                    break;

                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(type),
                        type,
                        "Saver.SetUpgradeLevel received an unknown upgrade type.");
            }

            SaveChanges();
        }

        public bool IsPerkPurchased(PerkType type)
        {
            return LoadData().PurchasedPerks.Contains(type);
        }

        public void PurchasePerk(PerkType type)
        {
            List<PerkType> purchasedPerks = LoadData().PurchasedPerks;

            if (purchasedPerks.Contains(type) == true)
            {
                throw new InvalidOperationException(
                    $"Saver.PurchasePerk: perk '{type}' is already purchased.");
            }

            purchasedPerks.Add(type);
            SaveChanges();
        }

        public void SelectSkin(string skinId)
        {
            if (string.IsNullOrEmpty(skinId) == true)
            {
                throw new ArgumentException(
                    "Saver.SelectSkin requires a non-empty skin id.",
                    nameof(skinId));
            }

            LoadData().SelectedSkinId = skinId;
            SaveChanges();
        }

        public bool IsSkinOpen(string skinId)
        {
            return LoadData()._openSkinIds.Contains(skinId);
        }

        public void OpenSkin(string skinId)
        {
            List<string> openSkinIds = LoadData()._openSkinIds;

            if (openSkinIds.Contains(skinId) == true)
            {
                throw new InvalidOperationException(
                    $"Saver.OpenSkin: skin '{skinId}' is already open.");
            }

            openSkinIds.Add(skinId);
            SaveChanges();
        }

        public List<string> ReadOpenSkinIds()
        {
            return new List<string>(LoadData()._openSkinIds);
        }

        public List<string> ReadShowcaseSkinIds()
        {
            return new List<string>(LoadData()._showcaseSkinIds);
        }

        public void SetShowcaseSkinIds(IReadOnlyList<string> showcaseSkinIds)
        {
            if (showcaseSkinIds == null)
            {
                throw new ArgumentNullException(nameof(showcaseSkinIds),
                    "Saver.SetShowcaseSkinIds requires a showcase id list.");
            }

            List<string> savedShowcase = LoadData()._showcaseSkinIds;

            savedShowcase.Clear();

            for (int i = 0; i < showcaseSkinIds.Count; i++)
            {
                savedShowcase.Add(showcaseSkinIds[i]);
            }

            SaveChanges();
        }

        public void SetLastFreeSpinUnixTime(long unixTime)
        {
            LoadData().LastFreeSpinUnixTime = unixTime;
            SaveChanges();
        }

        public int CountAdSpinsSince(long windowStartUnixTime)
        {
            List<long> spinTimes = LoadData().RouletteAdSpinTimes;
            int count = 0;

            for (int i = 0; i < spinTimes.Count; i++)
            {
                if (spinTimes[i] >= windowStartUnixTime)
                {
                    count++;
                }
            }

            return count;
        }

        public void PruneAdSpinsBefore(long cutoffUnixTime)
        {
            List<long> spinTimes = LoadData().RouletteAdSpinTimes;
            bool removedAny = false;

            for (int i = spinTimes.Count - 1; i >= 0; i--)
            {
                if (spinTimes[i] < cutoffUnixTime)
                {
                    spinTimes.RemoveAt(i);
                    removedAny = true;
                }
            }

            if (removedAny == true)
            {
                SaveChanges();
            }
        }

        public void RegisterAdSpin(long unixTime)
        {
            LoadData().RouletteAdSpinTimes.Add(unixTime);
            SaveChanges();
        }

        public void SetSkinSpinCount(int count)
        {
            if (count < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count),
                    count,
                    "Saver.SetSkinSpinCount requires a non-negative spin count.");
            }

            LoadData().SkinSpinCount = count;
            SaveChanges();
        }

        public void SetLanguage(string language)
        {
            if (language == null)
            {
                throw new ArgumentNullException(nameof(language),
                    "Saver.SetLanguage requires a language code.");
            }

            LoadData().Language = language;
            SaveChanges();
        }

        private bool _isTransactionRunning;

        public void ExecuteTransaction(Action operation)
        {
            if (operation == null)
            {
                throw new ArgumentNullException(nameof(operation));
            }

            if (_isTransactionRunning == true)
            {
                throw new InvalidOperationException("Saver: nested transactions are not supported.");
            }

            SavesYG originalData = LoadData();
            string snapshot = JsonUtility.ToJson(originalData);
            _isTransactionRunning = true;

            try
            {
                operation.Invoke();
                _access.Save();
            }
            catch
            {
                JsonUtility.FromJsonOverwrite(snapshot, originalData);
                throw;
            }
            finally
            {
                _isTransactionRunning = false;
            }
        }

        private void SaveChanges()
        {
            if (_isTransactionRunning == false)
            {
                _access.Save();
            }
        }

        private SavesYG LoadData()
        {
            if (_access.IsReady == false)
            {
                throw new InvalidOperationException(
                    "Saver: the save data is not ready yet. Wait for the Ready event before reading or writing saves.");
            }

            return _access.Data;
        }
    }
}
