using Audio;
using Game;
using Shop;
using Saves;
using Skins;
using Scriptables;
using Roulette;
using System;
using UI;
using System.Collections.Generic;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace DI
{
    public class MenuLifetimeScope : LifetimeScope
    {
        [SerializeField] private MainMenu _mainMenu;
        [SerializeField] private MenuSessionHandler _menuSessionHandler;
        [SerializeField] private RouletteConfig _rouletteConfig;
        [SerializeField] private ShopContent _shopContent;
        [SerializeField] private RoulettePresenter _roulettePresenter;
        [SerializeField] private AdScheduler _adScheduler;
        [SerializeField] private Wallet _wallet;
        [SerializeField] private Pauser _pauser;
        [SerializeField] private LevelLabelUI _levelLabelUI;

        [SerializeField] private List<UIButtonSound> _sceneButtonSounds;

        protected override void Configure(IContainerBuilder builder)
        {
            ValidateAssigned(_mainMenu, nameof(_mainMenu));
            ValidateAssigned(_menuSessionHandler, nameof(_menuSessionHandler));
            ValidateAssigned(_rouletteConfig, nameof(_rouletteConfig));
            ValidateAssigned(_shopContent, nameof(_shopContent));
            ValidateAssigned(_roulettePresenter, nameof(_roulettePresenter));
            ValidateAssigned(_adScheduler, nameof(_adScheduler));
            ValidateAssigned(_wallet, nameof(_wallet));
            ValidateAssigned(_pauser, nameof(_pauser));
            ValidateAssigned(_levelLabelUI, nameof(_levelLabelUI));

            builder.RegisterComponent(_mainMenu);
            builder.RegisterComponent(_menuSessionHandler);
            builder.Register<RouletteFactory>(resolver => new RouletteFactory(_rouletteConfig,
                resolver.Resolve<IRouletteStorage>(), resolver.Resolve<ISkinStorage>(), resolver.Resolve<SkinInventory>(),
                resolver.Resolve<Wallet>(), resolver.Resolve<Saver>().ExecuteTransaction), Lifetime.Scoped);
            builder.RegisterComponent(_roulettePresenter);
            builder.RegisterInstance(_shopContent);
            builder.RegisterInstance(_rouletteConfig);
            builder.RegisterComponent(_adScheduler);
            builder.RegisterComponent(_wallet);
            builder.RegisterComponent(_pauser);
            builder.RegisterComponent(_levelLabelUI);

            builder.Register<UiSpawner>(Lifetime.Scoped);

            builder.RegisterBuildCallback(InitializeScene);
        }

        private void InitializeScene(IObjectResolver container)
        {
            container.Resolve<SceneNavigator>().EnsureInitialized();
            container.Resolve<Pauser>().ResetToPlay();

            for (int i = 0; i < _sceneButtonSounds.Count; i++)
            {
                container.Inject(_sceneButtonSounds[i]);
            }
        }

        private void ValidateAssigned(object dependency, string fieldName)
        {
            if (dependency == null)
            {
                throw new InvalidOperationException(
                    $"MenuLifetimeScope: '{fieldName}' is not assigned. Select the DI object in the scene and drag the missing reference.");
            }
        }
    }
}
