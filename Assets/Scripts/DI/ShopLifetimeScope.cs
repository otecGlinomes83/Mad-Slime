using Game;
using UnityEngine;
using VContainer;
using VContainer.Unity;
using System;

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
        }
    }
}
