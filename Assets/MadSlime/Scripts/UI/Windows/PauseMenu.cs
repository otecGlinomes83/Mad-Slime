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
        [SerializeField] private RectTransform _languageButton;
        [SerializeField] private Vector2 _languagePositionWithMenuButton;
        [SerializeField] private Vector2 _languagePositionWithoutMenuButton;
        [SerializeField] private AudioSettingsPanel _settingsPanel;
        [SerializeField] private Button _resetButton;
        [SerializeField] private ResetProgressConfirmation _resetConfirmation;
        [SerializeField] private CanvasGroup _settingsInput;

        private bool _isActionRequested;
        private bool _isResetAvailable;

        public event Action CloseButtonClicked;

        public event Action MenuButtonClicked;
        public event Action ResetConfirmed;

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

            if (_languageButton == null)
            {
                throw new InvalidOperationException($"{name}: assign the language button RectTransform.");
            }

            if (_resetButton == null || _resetConfirmation == null || _settingsInput == null)
            {
                throw new InvalidOperationException($"{name}: assign the reset button, confirmation and settings input group.");
            }
        }

        public void Initialize(bool isMenuButtonNeeded, bool isResetAvailable = false)
        {
            _isResetAvailable = isResetAvailable;
            _isActionRequested = false;
            _closeButton.interactable = true;
            _menuButton.interactable = true;
            _settingsPanel.Initialize();
            _resetButton.gameObject.SetActive(isResetAvailable);
            _resetButton.interactable = true;
            OnResetCanceled();

            _menuButton.gameObject.SetActive(isMenuButtonNeeded);
            if (TryGetComponent(out ResponsiveCanvasLayout profiles) && profiles.Manages(_languageButton))
            {
                return;
            }

            if (isMenuButtonNeeded == true || isResetAvailable == true)
            {
                _languageButton.anchoredPosition = _languagePositionWithMenuButton;
            }
            else
            {
                _languageButton.anchoredPosition = _languagePositionWithoutMenuButton;
            }
        }

        private void OnEnable()
        {
            _closeButton.onClick.AddListener(OnCloseClicked);
            _menuButton.onClick.AddListener(OnMenuClicked);
            _resetButton.onClick.AddListener(OnResetClicked);
            _resetConfirmation.Confirmed += OnResetConfirmed;
            _resetConfirmation.Canceled += OnResetCanceled;
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            _closeButton.onClick.RemoveListener(OnCloseClicked);
            _menuButton.onClick.RemoveListener(OnMenuClicked);
            _resetButton.onClick.RemoveListener(OnResetClicked);
            _resetConfirmation.Confirmed -= OnResetConfirmed;
            _resetConfirmation.Canceled -= OnResetCanceled;
        }

        protected override void OnClosing()
        {
            _closeButton.interactable = false;
            _menuButton.interactable = false;
            _resetButton.interactable = false;
        }

        private void OnResetClicked()
        {
            if (_isResetAvailable == false || _isActionRequested == true || IsClosing == true)
            {
                return;
            }

            _settingsInput.interactable = false;
            _settingsInput.blocksRaycasts = false;
            _resetConfirmation.Show();
        }

        private void OnResetCanceled()
        {
            _resetConfirmation.Hide();
            _settingsInput.interactable = true;
            _settingsInput.blocksRaycasts = true;

            if (_resetButton.gameObject.activeInHierarchy == true)
            {
                _resetButton.Select();
            }
        }

        private void OnResetConfirmed()
        {
            if (_isResetAvailable == false || _isActionRequested == true || IsClosing == true)
            {
                return;
            }

            _isActionRequested = true;
            _resetConfirmation.Hide();
            ResetConfirmed?.Invoke();
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
