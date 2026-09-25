using Audio;
using Game;
using Roulette;
using Skins;
using System;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace DI
{
    public sealed class ShopLifetimeScope : LifetimeScope
    {
        [SerializeField] private Shop _shop;
        [SerializeField] private ShopPanel _shopPanel;
        [SerializeField] private ShopItemViewFactory _shopItemViewFactory;
        [SerializeField] private UpgradeItemViewFactory _upgradeItemViewFactory;
        [SerializeField] private ModelPlacer _modelPlacer;
        [SerializeField] private RouletteService _rouletteService;
        [SerializeField] private RouletteView _rouletteView;
        [SerializeField] private AdScheduler _adScheduler;
        [SerializeField] private Wallet _wallet;
        [SerializeField] private Pauser _pauser;

        protected override void Configure(IContainerBuilder builder)
        {
            ValidateAssigned(_shop, nameof(_shop));
            ValidateAssigned(_shopPanel, nameof(_shopPanel));
            ValidateAssigned(_shopItemViewFactory, nameof(_shopItemViewFactory));
            ValidateAssigned(_upgradeItemViewFactory, nameof(_upgradeItemViewFactory));
            ValidateAssigned(_modelPlacer, nameof(_modelPlacer));
            ValidateAssigned(_rouletteService, nameof(_rouletteService));
            ValidateAssigned(_rouletteView, nameof(_rouletteView));
            ValidateAssigned(_adScheduler, nameof(_adScheduler));
            ValidateAssigned(_wallet, nameof(_wallet));
            ValidateAssigned(_pauser, nameof(_pauser));

            builder.RegisterComponent(_shop);
            builder.RegisterComponent(_shopPanel);
            builder.RegisterComponent(_shopItemViewFactory);
            builder.RegisterComponent(_upgradeItemViewFactory);
            builder.RegisterComponent(_modelPlacer);
            builder.RegisterComponent(_rouletteService);
            builder.RegisterComponent(_rouletteView);
            builder.RegisterComponent(_adScheduler);
            builder.RegisterBuildCallback(InjectSceneButtonSounds);
            builder.RegisterComponent(_wallet);
            builder.RegisterComponent(_pauser);
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
                    $"ShopLifetimeScope: '{fieldName}' is not assigned. Select the DI object in the scene and drag the missing reference.");
            }
        }
    }
}
