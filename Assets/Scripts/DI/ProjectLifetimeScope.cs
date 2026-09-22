using Audio;
using Game;
using Scriptables;
using System;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace DI
{
    public sealed class ProjectLifetimeScope : LifetimeScope
    {
        [SerializeField] private PlayerProgress _playerProgress;
        [SerializeField] private LevelsCatalog _levelsCatalog;
        [SerializeField] private LocalizationService _localizationService;
        [SerializeField] private TierTable _tierTable;
        [SerializeField] private LayoutsLibrary _layoutsLibrary;
        [SerializeField] private SfxPlayer _sfxPlayer;
        [SerializeField] private MusicPlayer _musicPlayer;
        [SerializeField] private AudioMixerController _audioMixerController;

        protected override void Configure(IContainerBuilder builder)
        {
            if (_playerProgress == null)
            {
                throw new InvalidOperationException(
                    "ProjectLifetimeScope: PlayerProgress is not assigned. Open the ProjectScope prefab and drag the PlayerProgress component into the Player Progress field.");
            }

            if (_levelsCatalog == null)
            {
                throw new InvalidOperationException(
                    "ProjectLifetimeScope: LevelsCatalog is not assigned. Open the ProjectScope prefab and drag the LevelsCatalog asset into the Levels Catalog field.");
            }

            if (_localizationService == null)
            {
                throw new InvalidOperationException(
                    "ProjectLifetimeScope: LocalizationService is not assigned. Open the ProjectScope prefab and drag the LocalizationService component into the Localization Service field.");
            }

            if (_tierTable == null)
            {
                throw new InvalidOperationException(
                    "ProjectLifetimeScope: TierTable is not assigned. Open the ProjectScope prefab and drag the TierTable asset into the Tier Table field.");
            }

            if (_layoutsLibrary == null)
            {
                throw new InvalidOperationException(
                    "ProjectLifetimeScope: LayoutsLibrary is not assigned. Open the ProjectScope prefab and drag the LayoutsLibrary asset into the Layouts Library field.");
            }

            if (_sfxPlayer == null)
            {
                throw new InvalidOperationException(
                    "ProjectLifetimeScope: SfxPlayer is not assigned. Open the ProjectScope prefab and drag the SfxPlayer component into the Sfx Player field.");
            }

            if (_musicPlayer == null)
            {
                throw new InvalidOperationException(
                    "ProjectLifetimeScope: MusicPlayer is not assigned. Open the ProjectScope prefab and drag the MusicPlayer component into the Music Player field.");
            }

            if (_audioMixerController == null)
            {
                throw new InvalidOperationException(
                    "ProjectLifetimeScope: AudioMixerController is not assigned. Open the ProjectScope prefab and drag the AudioMixerController component into the Audio Mixer Controller field.");
            }

            builder.RegisterComponent(_playerProgress);
            builder.RegisterComponent(_localizationService);
            builder.RegisterComponent(_sfxPlayer);
            builder.RegisterComponent(_musicPlayer);
            builder.RegisterComponent(_audioMixerController);
            builder.RegisterInstance(_levelsCatalog);
            builder.RegisterInstance(_tierTable);
            builder.RegisterInstance(_layoutsLibrary);
            builder.Register<LevelProgress>(Lifetime.Singleton);
            builder.Register<LevelConfigResolver>(Lifetime.Singleton);
            builder.RegisterEntryPoint<SessionStateLogger>(Lifetime.Singleton);
        }
    }
}
