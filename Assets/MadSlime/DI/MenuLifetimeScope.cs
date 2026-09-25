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

        protected override void Configure(IContainerBuilder builder)
        {
            ValidateAssigned(_mainMenu, nameof(_mainMenu));
            ValidateAssigned(_startup, nameof(_startup));
            ValidateAssigned(_rouletteService, nameof(_rouletteService));
            ValidateAssigned(_rouletteView, nameof(_rouletteView));
            ValidateAssigned(_adScheduler, nameof(_adScheduler));
            ValidateAssigned(_wallet, nameof(_wallet));
            ValidateAssigned(_pauser, nameof(_pauser));

            builder.RegisterComponent(_mainMenu);
            builder.RegisterComponent(_startup);
            builder.RegisterComponent(_rouletteService);
            builder.RegisterComponent(_rouletteView);
            builder.RegisterComponent(_adScheduler);
            builder.RegisterComponent(_wallet);
            builder.RegisterComponent(_pauser);

            builder.RegisterBuildCallback(InjectSceneButtonSounds);
        }

        private void InjectSceneButtonSounds(IObjectResolver container)
        {
            GameObject[] sceneRoots = gameObject.scene.GetRootGameObjects();

            for (int rootIndex = 0; rootIndex < sceneRoots.Length; rootIndex++)
            {
                UIButtonSound[] buttonSounds = sceneRoots[rootIndex].GetComponentsInChildren<UIButtonSound>(true);

                for (int soundIndex = 0; soundIndex < buttonSounds.Length; soundIndex++)
                {
                    container.Inject(buttonSounds[soundIndex]);
                }
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
