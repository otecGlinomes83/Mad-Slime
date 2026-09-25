using Audio;
using Game;
using Scriptables;
using ShapeFill;
using System;
using UI;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace DI
{
    public sealed class FillLifetimeScope : LifetimeScope
    {
        [SerializeField] private FillConfig _fillConfig;
        [SerializeField] private FillSessionHandler _fillSessionHandler;
        [SerializeField] private ShapeFillOrchestrator _fillOrchestrator;
        [SerializeField] private GridBuilder _gridBuilder;
        [SerializeField] private ShapeFiller _shapeFiller;
        [SerializeField] private CubeSpawner _cubeSpawner;
        [SerializeField] private FillCounter _fillCounter;
        [SerializeField] private FillTapInput _fillTapInput;
        [SerializeField] private FillFinale _fillFinale;
        [SerializeField] private FillProgressUI _fillProgressUI;
        [SerializeField] private FillDebug _fillDebug;
        [SerializeField] private Rewarder _rewarder;
        [SerializeField] private AdScheduler _adScheduler;
        [SerializeField] private LeaderboardReporter _leaderboardReporter;
        [SerializeField] private Wallet _wallet;
        [SerializeField] private Pauser _pauser;
        [SerializeField] private FillUIFabric _fillUIFabric;
        [SerializeField] private FlyingCubeArrivalSound _flyingCubeArrivalSound;

        protected override void Configure(IContainerBuilder builder)
        {
            ValidateAssigned(_fillConfig, nameof(_fillConfig));
            ValidateAssigned(_fillSessionHandler, nameof(_fillSessionHandler));
            ValidateAssigned(_fillOrchestrator, nameof(_fillOrchestrator));
            ValidateAssigned(_gridBuilder, nameof(_gridBuilder));
            ValidateAssigned(_shapeFiller, nameof(_shapeFiller));
            ValidateAssigned(_cubeSpawner, nameof(_cubeSpawner));
            ValidateAssigned(_fillCounter, nameof(_fillCounter));
            ValidateAssigned(_fillTapInput, nameof(_fillTapInput));
            ValidateAssigned(_fillFinale, nameof(_fillFinale));
            ValidateAssigned(_fillProgressUI, nameof(_fillProgressUI));
            ValidateAssigned(_fillDebug, nameof(_fillDebug));
            ValidateAssigned(_rewarder, nameof(_rewarder));
            ValidateAssigned(_adScheduler, nameof(_adScheduler));
            ValidateAssigned(_leaderboardReporter, nameof(_leaderboardReporter));
            ValidateAssigned(_wallet, nameof(_wallet));
            ValidateAssigned(_pauser, nameof(_pauser));
            ValidateAssigned(_fillUIFabric, nameof(_fillUIFabric));
            ValidateAssigned(_flyingCubeArrivalSound, nameof(_flyingCubeArrivalSound));

            builder.RegisterInstance(_fillConfig);
            builder.RegisterComponent(_fillSessionHandler);
            builder.RegisterComponent(_fillOrchestrator);
            builder.RegisterComponent(_gridBuilder);
            builder.RegisterComponent(_shapeFiller);
            builder.RegisterComponent(_cubeSpawner);
            builder.RegisterComponent(_fillCounter);
            builder.RegisterComponent(_fillTapInput);
            builder.RegisterComponent(_fillFinale);
            builder.RegisterComponent(_fillProgressUI);
            builder.RegisterComponent(_fillDebug);
            builder.RegisterComponent(_rewarder);
            builder.RegisterComponent(_adScheduler);
            builder.RegisterComponent(_leaderboardReporter);
            builder.RegisterComponent(_wallet);
            builder.RegisterComponent(_pauser);
            builder.RegisterBuildCallback(InjectSceneButtonSounds);
            builder.RegisterComponent(_fillUIFabric);
            builder.RegisterComponent(_flyingCubeArrivalSound);
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
                    $"FillLifetimeScope: '{fieldName}' is not assigned. Select the DI object in the scene and drag the missing reference.");
            }
        }
    }
}
