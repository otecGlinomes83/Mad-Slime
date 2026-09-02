using Player;
using System.Collections.Generic;
using UnityEngine;
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
