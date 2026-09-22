using System;
using Audio;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public sealed class PauseMenu : BaseWindow
    {
        [SerializeField] private Button _closeButton;
        [SerializeField] private Button _menuButton;
        [SerializeField] private AudioSettingsPanel _settingsPanel;

        private Action _menuAction;

        public void Initialize(bool isMenuButtonNeeded, Action menuAction = null)
        {
            base.Initialize();

            if (_closeButton == null)
            {
                throw new InvalidOperationException(
                    $"{name}: CloseButton is not assigned. Drag a Button into the _closeButton field.");
            }

            if (_menuButton == null)
            {
                throw new InvalidOperationException(
                    $"{name}: MenuButton is not assigned. Drag a Button into the _menuButton field.");
            }

            if (_settingsPanel == null)
            {
                throw new InvalidOperationException(
                    $"{name}: SettingsPanel is not assigned. Drag an AudioSettingsPanel into the _settingsPanel field.");
            }

            _closeButton.onClick.AddListener(Close);
            _menuButton.gameObject.SetActive(false);

            if (isMenuButtonNeeded && menuAction != null)
            {
                _menuButton.gameObject.SetActive(true);
                _menuButton.onClick.AddListener(OnMenuClicked);
                _menuAction = menuAction;
            }

            _settingsPanel.Initialize();
        }

        protected override void OnDisable()
        {
            _closeButton?.onClick.RemoveListener(Close);
            _menuButton?.onClick.RemoveListener(OnMenuClicked);
            base.OnDisable();
        }

        private void OnMenuClicked()
        {
            _menuAction?.Invoke();
            Close();
        }

        private void Close()
        {
            Destroy(gameObject);
        }
    }
}