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
        [SerializeField] private AdScheduler _adScheduler;
        [SerializeField] private UIButtonSound[] _uiButtonSounds;
        [SerializeField] private LevelTransitor _levelTransitor;
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
            ValidateAssigned(_adScheduler, nameof(_adScheduler));
            ValidateButtons(_uiButtonSounds);
            ValidateAssigned(_levelTransitor, nameof(_levelTransitor));
            ValidateAssigned(_wallet, nameof(_wallet));
            ValidateAssigned(_pauser, nameof(_pauser));

            builder.RegisterComponent(_shop);
            builder.RegisterComponent(_shopPanel);
            builder.RegisterComponent(_shopItemViewFactory);
            builder.RegisterComponent(_upgradeItemViewFactory);
            builder.RegisterComponent(_modelPlacer);
            builder.RegisterComponent(_rouletteService);
            builder.RegisterComponent(_adScheduler);
            builder.RegisterBuildCallback(InjectButtonSounds);
            builder.RegisterComponent(_levelTransitor);
            builder.RegisterComponent(_wallet);
            builder.RegisterComponent(_pauser);
        }

        private void InjectButtonSounds(IObjectResolver container)
        {
            for (int index = 0; index < _uiButtonSounds.Length; index++)
            {
                container.Inject(_uiButtonSounds[index]);
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

        private void ValidateButtons(UIButtonSound[] buttons)
        {
            if (buttons == null)
            {
                throw new InvalidOperationException(
                    $"ShopLifetimeScope: '{nameof(_uiButtonSounds)}' is not assigned. An empty list is valid (no static UI buttons), a missing list is not.");
            }

            for (int index = 0; index < buttons.Length; index++)
            {
                if (buttons[index] == null)
                {
                    throw new InvalidOperationException(
                        $"ShopLifetimeScope: '{nameof(_uiButtonSounds)}' element {index} is empty. Drag a UIButtonSound component into every slot.");
                }
            }
        }
    }
}
