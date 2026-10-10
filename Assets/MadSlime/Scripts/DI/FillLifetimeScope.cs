using Audio;
using Game;
using Scriptables;
using ShapeFill;
using System;
using UI;
using System.Collections.Generic;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace DI
{
    public class FillLifetimeScope : LifetimeScope
    {
        [SerializeField] private FillConfig _fillConfig;
        [SerializeField] private FillSessionHandler _fillSessionHandler;
        [SerializeField] private ShapeFillOrchestrator _fillOrchestrator;
        [SerializeField] private GridBuilder _gridBuilder;
        [SerializeField] private ShapeFiller _shapeFiller;
        [SerializeField] private CubeSpawner _cubeSpawner;
        [SerializeField] private FillResultCalculator _fillResultCalculator;
        [SerializeField] private FillTapButton _fillTapButton;
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
            ValidateAssigned(_fillResultCalculator, nameof(_fillResultCalculator));
            ValidateAssigned(_fillTapButton, nameof(_fillTapButton));
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
            builder.Register<UiSpawner>(Lifetime.Scoped);
            builder.RegisterComponent(_fillSessionHandler);
            builder.RegisterComponent(_fillOrchestrator);
            builder.RegisterComponent(_gridBuilder);
            builder.RegisterComponent(_shapeFiller);
            builder.RegisterComponent(_cubeSpawner);
            builder.RegisterComponent(_fillResultCalculator);
            builder.RegisterComponent(_fillTapButton);
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

        [SerializeField] private List<UIButtonSound> _sceneButtonSounds;

        private void InjectSceneButtonSounds(IObjectResolver container)
        {
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
                    $"FillLifetimeScope: '{fieldName}' is not assigned. Select the DI object in the scene and drag the missing reference.");
            }
        }
    }
}
