using Audio;
using Game;
using Roulette;
using Shop;
using System;
using UI;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace DI
{
    public sealed class ShopLifetimeScope : LifetimeScope
    {
        [SerializeField] private ShopPanel _shopPanel;
        [SerializeField] private ShopItemViewFactory _shopItemViewFactory;
        [SerializeField] private UpgradeItemViewFactory _upgradeItemViewFactory;
        [SerializeField] private ModelPlacer _modelPlacer;
        [SerializeField] private RouletteService _rouletteService;
        [SerializeField] private RouletteView _rouletteView;
        [SerializeField] private AdScheduler _adScheduler;
        [SerializeField] private Wallet _wallet;
        [SerializeField] private Pauser _pauser;
        [SerializeField] private ShopCloseButton _shopCloseButton;
        [SerializeField] private ShopMusic _shopMusic;

        protected override void Configure(IContainerBuilder builder)
        {
            ValidateAssigned(_shopPanel, nameof(_shopPanel));
            ValidateAssigned(_shopItemViewFactory, nameof(_shopItemViewFactory));
            ValidateAssigned(_upgradeItemViewFactory, nameof(_upgradeItemViewFactory));
            ValidateAssigned(_modelPlacer, nameof(_modelPlacer));
            ValidateAssigned(_rouletteService, nameof(_rouletteService));
            ValidateAssigned(_rouletteView, nameof(_rouletteView));
            ValidateAssigned(_adScheduler, nameof(_adScheduler));
            ValidateAssigned(_wallet, nameof(_wallet));
            ValidateAssigned(_pauser, nameof(_pauser));
            ValidateAssigned(_shopCloseButton, nameof(_shopCloseButton));
            ValidateAssigned(_shopMusic, nameof(_shopMusic));

            builder.RegisterComponent(_shopPanel);
            builder.RegisterComponent(_shopItemViewFactory);
            builder.RegisterComponent(_upgradeItemViewFactory);
            builder.RegisterComponent(_modelPlacer);
            builder.RegisterComponent(_rouletteService);
            builder.RegisterComponent(_rouletteView);
            builder.RegisterComponent(_adScheduler);
            builder.Register<UiSpawner>(Lifetime.Scoped);
            builder.RegisterBuildCallback(InjectSceneButtonSounds);
            builder.RegisterComponent(_wallet);
            builder.RegisterComponent(_pauser);
            builder.RegisterComponent(_shopCloseButton);
            builder.RegisterComponent(_shopMusic);
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
                    $"ShopLifetimeScope: '{fieldName}' is not assigned. Select the DI object in the scene and drag the missing reference.");
            }
        }
    }
}
