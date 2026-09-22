using Audio;
using Game;
using System;
using UI;
using UnityEngine;
using VContainer;

namespace DI
{
    public sealed class MenuLifetimeScope : LifetimeScope
    {
        [SerializeField] private MainMenu _mainMenu;
        [SerializeField] private LevelTransitor _levelTransitor;
        [SerializeField] private Pauser _pauser;
        [SerializeField] private AudioMixerController _audioMixerController;
        [SerializeField] private UIButtonSound[] _uiButtonSounds;

        protected override void Configure(IContainerBuilder builder)
        {
            ValidateAssigned(_mainMenu, nameof(_mainMenu));
            ValidateAssigned(_levelTransitor, nameof(_levelTransitor));
            ValidateAssigned(_pauser, nameof(_pauser));
            ValidateAssigned(_audioMixerController, nameof(_audioMixerController));
            ValidateButtons(_uiButtonSounds);

            builder.RegisterComponent(_mainMenu);
            builder.RegisterComponent(_levelTransitor);
            builder.RegisterComponent(_pauser);
            builder.RegisterComponent(_audioMixerController);

            for (int index = 0; index < _uiButtonSounds.Length; index++)
            {
                builder.RegisterComponent(_uiButtonSounds[index]);
            }
        }

        private void ValidateAssigned(object dependency, string fieldName)
        {
            if (dependency == null)
            {
                throw new InvalidOperationException(
                    $"MenuLifetimeScope: '{fieldName}' is not assigned. Select the DI object in the scene and drag the missing reference.");
            }
        }

        private void ValidateButtons(UIButtonSound[] buttons)
        {
            if (buttons == null || buttons.Length == 0)
            {
                throw new InvalidOperationException(
                    $"MenuLifetimeScope: '{nameof(_uiButtonSounds)}' is empty. Drag every UIButtonSound component of the scene into the list.");
            }

            for (int index = 0; index < buttons.Length; index++)
            {
                if (buttons[index] == null)
                {
                    throw new InvalidOperationException(
                        $"MenuLifetimeScope: '{nameof(_uiButtonSounds)}' element {index} is empty. Drag a UIButtonSound component into every slot.");
                }
            }
        }
    }
}
