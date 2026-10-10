using System;
using Audio;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class PauseMenu : BaseWindow
    {
        [SerializeField] private Button _closeButton;
        [SerializeField] private Button _menuButton;
        [SerializeField] private AudioSettingsPanel _settingsPanel;

        private bool _isActionRequested;

        public event Action CloseButtonClicked;

        public event Action MenuButtonClicked;

        protected override void Awake()
        {
            base.Awake();

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
        }

        public void Initialize(bool isMenuButtonNeeded)
        {
            _isActionRequested = false;
            _closeButton.interactable = true;
            _menuButton.interactable = true;
            _settingsPanel.Initialize();

            _menuButton.gameObject.SetActive(isMenuButtonNeeded);
        }

        private void OnEnable()
        {
            _closeButton.onClick.AddListener(OnCloseClicked);
            _menuButton.onClick.AddListener(OnMenuClicked);
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            _closeButton.onClick.RemoveListener(OnCloseClicked);
            _menuButton.onClick.RemoveListener(OnMenuClicked);
        }

        protected override void OnClosing()
        {
            _closeButton.interactable = false;
            _menuButton.interactable = false;
        }

        private void OnCloseClicked()
        {
            if (_isActionRequested == true || IsClosing == true)
            {
                return;
            }

            _isActionRequested = true;
            Action closeButtonClicked = CloseButtonClicked;
            closeButtonClicked?.Invoke();
        }

        private void OnMenuClicked()
        {
            if (_isActionRequested == true || IsClosing == true)
            {
                return;
            }

            _isActionRequested = true;
            Action menuButtonClicked = MenuButtonClicked;
            menuButtonClicked?.Invoke();
        }
    }
}
