using Audio;
using Game;
using Skins;
using System;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace DI
{
    public sealed class ShopLifetimeScope : LifetimeScope
    {
        [SerializeField] private Wallet _wallet;

        protected override void Configure(IContainerBuilder builder)
        {
            if (_wallet == null)
            {
                throw new InvalidOperationException(
                    "ShopLifetimeScope: '_wallet' is not assigned. Select the DI object in the scene and drag the Wallet reference.");
            }

            builder.RegisterComponent(_wallet);

            builder.RegisterComponentInHierarchy<Shop>();
            builder.RegisterComponentInHierarchy<ShopItemViewFactory>();

            builder.RegisterBuildCallback(
                container =>
                {
                    foreach (UIButtonSound sound in FindObjectsOfType<UIButtonSound>(true))
                    {
                        container.Inject(sound);
                    }
                });
        }
    }
}