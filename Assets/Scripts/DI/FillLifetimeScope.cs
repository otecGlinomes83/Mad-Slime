using Game;
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
        [SerializeField] private FillSessionHandler _fillSessionHandler;
        [SerializeField] private FillCounter _fillCounter;
        [SerializeField] private FillUIFabric _fillUIFabric;
        [SerializeField] private Wallet _wallet;

        protected override void Configure(IContainerBuilder builder)
        {
            ValidateAssigned(_fillSessionHandler, nameof(_fillSessionHandler));
            ValidateAssigned(_fillCounter, nameof(_fillCounter));
            ValidateAssigned(_fillUIFabric, nameof(_fillUIFabric));
            ValidateAssigned(_wallet, nameof(_wallet));

            builder.RegisterComponent(_fillSessionHandler);
            builder.RegisterComponent(_fillCounter);
            builder.RegisterComponent(_fillUIFabric);
            builder.RegisterComponent(_wallet);
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
