using Adapters;
using Audio;
using Core;
using Game;
using Quota;
using Saves;
using Skins;
using Scriptables;
using System;
using UnityEngine;
using Upgrades;
using VContainer;
using VContainer.Unity;

namespace DI
{
    public class ProjectLifetimeScope : LifetimeScope
    {
        [SerializeField] private PlayerUpgrades _playerUpgrades;
        [SerializeField] private SaveConfirmation _saveConfirmation;
        [SerializeField] private LevelsCatalog _levelsCatalog;
        [SerializeField] private LocalizationService _localizationService;
        [SerializeField] private TierTable _tierTable;
        [SerializeField] private LayoutsLibrary _layoutsLibrary;
        [SerializeField] private AudioPlayer _audioPlayer;
        [SerializeField] private AudioMixerController _audioMixerController;
        [SerializeField] private SceneNavigator _sceneNavigator;

        protected override void Configure(IContainerBuilder builder)
        {
            if (_playerUpgrades == null)
            {
                throw new InvalidOperationException(
                    "ProjectLifetimeScope: PlayerUpgrades is not assigned. Open the ProjectScope prefab and drag the PlayerUpgrades component into the Player Upgrades field.");
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

            if (_audioPlayer == null)
            {
                throw new InvalidOperationException(
                    "ProjectLifetimeScope: AudioPlayer is not assigned. Open the ProjectScope prefab and drag the AudioPlayer component into the Audio Player field.");
            }

            if (_audioMixerController == null)
            {
                throw new InvalidOperationException(
                    "ProjectLifetimeScope: AudioMixerController is not assigned. Open the ProjectScope prefab and drag the AudioMixerController component into the Audio Mixer Controller field.");
            }

            if (_sceneNavigator == null)
            {
                throw new InvalidOperationException(
                    "ProjectLifetimeScope: SceneNavigator is not assigned. Open the ProjectScope prefab and drag the SceneNavigator component into the Scene Navigator field.");
            }

            Yg2SavesAccess savesAccess = new Yg2SavesAccess(_saveConfirmation);
            Saver saver = new Saver(savesAccess);

            builder.RegisterInstance(savesAccess);
            builder.RegisterInstance(saver);
            builder.RegisterInstance<ISaveConfirmation>(saver);
            builder.RegisterInstance<Action<Action>>(saver.ExecuteTransaction);
            builder.Register<SkinInventory>(Lifetime.Singleton);
            builder.RegisterInstance<Saves.ISavesReadiness>(saver);
            builder.RegisterInstance<Saves.IBalanceStorage>(saver);
            builder.RegisterInstance<Saves.ILevelStorage>(saver);
            builder.RegisterInstance<Saves.IAudioStorage>(saver);
            builder.RegisterInstance<Saves.IUpgradesStorage>(saver);
            builder.RegisterInstance<Saves.ISkinStorage>(saver);
            builder.RegisterInstance<Saves.IRouletteStorage>(saver);
            builder.RegisterInstance<Saves.ILanguageStorage>(saver);
            builder.RegisterInstance<Saves.ICollectedItemsStorage>(saver);

            builder.RegisterComponent(_playerUpgrades);
            builder.RegisterComponent(_localizationService);
            builder.RegisterComponent(_audioPlayer)
                .As<Audio.IMusicPlayer>()
                .As<Audio.IGameSoundPlayer>()
                .As<Audio.IUISoundPlayer>();
            builder.RegisterComponent(_audioMixerController);
            builder.RegisterComponent(_sceneNavigator);
            builder.RegisterInstance(_levelsCatalog);
            builder.RegisterInstance(_tierTable);
            builder.RegisterInstance(_layoutsLibrary);
            QuotaBoard quotaBoard = new QuotaBoard();
            QuotaCounter quotaCounter = new QuotaCounter(quotaBoard, _playerUpgrades);

            builder.RegisterInstance(quotaBoard);
            builder.RegisterInstance(quotaCounter);
            builder.Register<LevelConfigResolver>(Lifetime.Singleton);
            builder.Register<IAdsService>(resolver => new Yg2AdsService(), Lifetime.Singleton);
            builder.Register<ILeaderboardService>(resolver => new Yg2LeaderboardService(), Lifetime.Singleton);
            builder.Register<ILanguageProvider>(resolver => new Yg2LanguageProvider(), Lifetime.Singleton);
            builder.Register<IGameVisibility, Yg2GameVisibility>(Lifetime.Singleton);
            builder.Register<IGameplayReporter>(resolver => new Yg2GameplayReporter(), Lifetime.Singleton);
        }
    }
}
