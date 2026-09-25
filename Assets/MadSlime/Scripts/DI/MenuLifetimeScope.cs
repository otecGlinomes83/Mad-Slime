using Audio;
using Game;
using System;
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
        [SerializeField] private Pauser _pauser;
        [SerializeField] private UIButtonSound[] _uiButtonSounds;

        protected override void Configure(IContainerBuilder builder)
        {
            ValidateAssigned(_mainMenu, nameof(_mainMenu));
            ValidateAssigned(_levelTransitor, nameof(_levelTransitor));
            ValidateAssigned(_pauser, nameof(_pauser));
            ValidateButtons(_uiButtonSounds);

            builder.RegisterComponent(_mainMenu);
            builder.RegisterComponent(_levelTransitor);
            builder.RegisterComponent(_pauser);

            builder.RegisterBuildCallback(InjectButtonSounds);
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
                    $"MenuLifetimeScope: '{fieldName}' is not assigned. Select the DI object in the scene and drag the missing reference.");
            }
        }

        private void ValidateButtons(UIButtonSound[] buttons)
        {
            if (buttons == null)
            {
                throw new InvalidOperationException(
                    $"MenuLifetimeScope: '{nameof(_uiButtonSounds)}' is not assigned. An empty list is valid (no static UI buttons), a missing list is not.");
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
