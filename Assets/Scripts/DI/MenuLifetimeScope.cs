using Game;
using System;
using Audio;
using UI;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace DI
{
    public sealed class MenuLifetimeScope : LifetimeScope
    {
        [SerializeField] private MainMenu _mainMenu;
        [SerializeField] private LevelTransitor _levelTransitor;
        [SerializeField] private Wallet _wallet;
        [SerializeField] private AudioMixerController _audioMixerController;
        [SerializeField] private Pauser _pauser;

        protected override void Configure(IContainerBuilder builder)
        {
            ValidateAssigned(_mainMenu, nameof(_mainMenu));
            ValidateAssigned(_levelTransitor, nameof(_levelTransitor));
            ValidateAssigned(_wallet, nameof(_wallet));
            ValidateAssigned(_audioMixerController, nameof(_audioMixerController));
            ValidateAssigned(_pauser, nameof(_pauser));
            
            builder.RegisterComponent(_mainMenu);
            builder.RegisterComponent(_levelTransitor);
            builder.RegisterComponent(_wallet);
            builder.RegisterComponent(_audioMixerController);
            builder.RegisterComponent(_pauser);
        }

        private void ValidateAssigned(object dependency, string fieldName)
        {
            if (dependency == null)
            {
                throw new InvalidOperationException(
                    $"MenuLifetimeScope: '{fieldName}' is not assigned. Select the DI object in the scene and drag the missing reference.");
            }
        }
    }
}