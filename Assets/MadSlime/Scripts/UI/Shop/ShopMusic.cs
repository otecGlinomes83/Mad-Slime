using Audio;
using Scriptables;
using System;
using UnityEngine;
using VContainer;

namespace Skins
{
    public sealed class ShopMusic : MonoBehaviour
    {
        [SerializeField] private SfxClip _musicTrack;

        private MusicPlayer _musicPlayer;

        [Inject]
        public void Construct(MusicPlayer musicPlayer)
        {
            _musicPlayer = musicPlayer;
        }

        private void Awake()
        {
            if (_musicTrack == null)
            {
                throw new InvalidOperationException(
                    $"{name}: Music track is not assigned. Drag a SfxClip asset into the _musicTrack field.");
            }

            if (_musicPlayer == null)
            {
                throw new InvalidOperationException(
                    $"{name}: MusicPlayer was not injected. Check that ProjectLifetimeScope registers MusicPlayer and ShopLifetimeScope registers ShopMusic.");
            }
        }

        private void Start()
        {
            _musicPlayer.Play(_musicTrack);
        }
    }
}
