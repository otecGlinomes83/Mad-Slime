using Audio;
using Game;
using Saves;
using Skins;
using Scriptables;
using Roulette;
using Shop;
using System;
using UI;
using System.Collections.Generic;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace DI
{
    public class ShopLifetimeScope : LifetimeScope
    {
        [SerializeField] private ShopPanel _shopPanel;
        [SerializeField] private ShopScenePresentation _presentation;
        [SerializeField] private ShopItemViewFactory _shopItemViewFactory;
        [SerializeField] private UpgradeItemViewFactory _upgradeItemViewFactory;
        [SerializeField] private ModelPlacer _modelPlacer;
        [SerializeField] private RouletteConfig _rouletteConfig;
        [SerializeField] private ShopContent _shopContent;
        [SerializeField] private RoulettePresenter _roulettePresenter;
        [SerializeField] private AdScheduler _adScheduler;
        [SerializeField] private Wallet _wallet;
        [SerializeField] private Pauser _pauser;
        [SerializeField] private ShopSessionHandler _shopSessionHandler;

        protected override void Configure(IContainerBuilder builder)
        {
            ValidateAssigned(_shopPanel, nameof(_shopPanel));
            ValidateAssigned(_shopItemViewFactory, nameof(_shopItemViewFactory));
            ValidateAssigned(_upgradeItemViewFactory, nameof(_upgradeItemViewFactory));
            ValidateAssigned(_modelPlacer, nameof(_modelPlacer));
            ValidateAssigned(_rouletteConfig, nameof(_rouletteConfig));
            ValidateAssigned(_shopContent, nameof(_shopContent));
            ValidateAssigned(_roulettePresenter, nameof(_roulettePresenter));
            ValidateAssigned(_adScheduler, nameof(_adScheduler));
            ValidateAssigned(_wallet, nameof(_wallet));
            ValidateAssigned(_pauser, nameof(_pauser));
            ValidateAssigned(_shopSessionHandler, nameof(_shopSessionHandler));

            builder.RegisterComponent(_shopPanel);
            builder.RegisterComponent(_presentation);
            builder.RegisterComponent(_shopItemViewFactory);
            builder.RegisterComponent(_upgradeItemViewFactory);
            builder.RegisterComponent(_modelPlacer);
            builder.Register<RouletteFactory>(resolver => new RouletteFactory(_rouletteConfig,
                resolver.Resolve<IRouletteStorage>(), resolver.Resolve<ISkinStorage>(), resolver.Resolve<SkinInventory>(),
                resolver.Resolve<Wallet>(), resolver.Resolve<Saver>().ExecuteTransaction), Lifetime.Scoped);
            builder.RegisterComponent(_roulettePresenter);
            builder.RegisterInstance(_shopContent);
            builder.RegisterInstance(_rouletteConfig);
            builder.RegisterComponent(_adScheduler);
            builder.Register<UiSpawner>(Lifetime.Scoped);
            builder.RegisterBuildCallback(InjectSceneButtonSounds);
            builder.RegisterComponent(_wallet);
            builder.RegisterComponent(_pauser);
            builder.RegisterComponent(_shopSessionHandler);
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
                    $"ShopLifetimeScope: '{fieldName}' is not assigned. Select the DI object in the scene and drag the missing reference.");
            }
        }
    }
}
