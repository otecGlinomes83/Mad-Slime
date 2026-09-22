using Audio;
using Game;
using Shop;
using System;
using UnityEngine;
using VContainer;

namespace DI
{
    public sealed class ShopLifetimeScope : LifetimeScope
    {
        [SerializeField] private Shop _shop;
        [SerializeField] private ShopPanel _shopPanel;
        [SerializeField] private ShopItemViewFactory _shopItemViewFactory;
        [SerializeField] private ModelPlacer _modelPlacer;
        [SerializeField] private UIButtonSound[] _uiButtonSounds;
        [SerializeField] private LevelTransitor _levelTransitor;
        [SerializeField] private Wallet _wallet;

        protected override void Configure(IContainerBuilder builder)
        {
            ValidateAssigned(_shop, nameof(_shop));
            ValidateAssigned(_shopPanel, nameof(_shopPanel));
            ValidateAssigned(_shopItemViewFactory, nameof(_shopItemViewFactory));
            ValidateAssigned(_modelPlacer, nameof(_modelPlacer));
            ValidateButtons(_uiButtonSounds);
            ValidateAssigned(_levelTransitor, nameof(_levelTransitor));
            ValidateAssigned(_wallet, nameof(_wallet));

            builder.RegisterComponent(_shop);
            builder.RegisterComponent(_shopPanel);
            builder.RegisterComponent(_shopItemViewFactory);
            builder.RegisterComponent(_modelPlacer);
            for (int index = 0; index < _uiButtonSounds.Length; index++)
            {
                builder.RegisterComponent(_uiButtonSounds[index]);
            }
            builder.RegisterComponent(_levelTransitor);
            builder.RegisterComponent(_wallet);
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
            if (buttons == null || buttons.Length == 0)
            {
                throw new InvalidOperationException(
                    $"ShopLifetimeScope: '{nameof(_uiButtonSounds)}' is empty. Drag every UIButtonSound component of the scene into the list.");
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
