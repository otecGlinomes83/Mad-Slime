using Audio;
using Game;
using Roulette;
using System;
using UI;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace DI
{
    public sealed class MenuLifetimeScope : LifetimeScope
    {
        [SerializeField] private MainMenu _mainMenu;
        [SerializeField] private Startup _startup;
        [SerializeField] private RouletteService _rouletteService;
        [SerializeField] private RouletteView _rouletteView;
        [SerializeField] private AdScheduler _adScheduler;
        [SerializeField] private Wallet _wallet;
        [SerializeField] private Pauser _pauser;
        [SerializeField] private LevelLabelUI _levelLabelUI;

        protected override void Configure(IContainerBuilder builder)
        {
            ValidateAssigned(_mainMenu, nameof(_mainMenu));
            ValidateAssigned(_startup, nameof(_startup));
            ValidateAssigned(_rouletteService, nameof(_rouletteService));
            ValidateAssigned(_rouletteView, nameof(_rouletteView));
            ValidateAssigned(_adScheduler, nameof(_adScheduler));
            ValidateAssigned(_wallet, nameof(_wallet));
            ValidateAssigned(_pauser, nameof(_pauser));
            ValidateAssigned(_levelLabelUI, nameof(_levelLabelUI));

            builder.RegisterComponent(_mainMenu);
            builder.RegisterComponent(_startup);
            builder.RegisterComponent(_rouletteService);
            builder.RegisterComponent(_rouletteView);
            builder.RegisterComponent(_adScheduler);
            builder.RegisterComponent(_wallet);
            builder.RegisterComponent(_pauser);
            builder.RegisterComponent(_levelLabelUI);

            builder.Register<UiSpawner>(Lifetime.Scoped);

            builder.RegisterBuildCallback(InjectSceneButtonSounds);
        }

        private void InjectSceneButtonSounds(IObjectResolver container)
        {
            UiButtonSoundInjector.InjectInScene(container, gameObject.scene);
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
