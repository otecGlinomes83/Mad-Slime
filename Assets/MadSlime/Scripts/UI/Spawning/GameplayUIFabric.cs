using Game;
using System;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace UI
{
    public class GameplayUIFabric : MonoBehaviour
    {
        [SerializeField] private Button _pauseButton;
        [SerializeField] private PauseMenu _pauseMenuPrefab;

        private UiSpawner _uiSpawner;
        private GameplaySessionHandler _sessionHandler;
        private PauseMenu _activePauseMenu;

        [Inject]
        public void Construct(UiSpawner uiSpawner, GameplaySessionHandler sessionHandler)
        {
            _uiSpawner = uiSpawner;
            _sessionHandler = sessionHandler;
        }

        private void Awake()
        {
            if (_uiSpawner == null)
            {
                throw new InvalidOperationException(
                    $"{name}: UiSpawner was not injected. Check that GameLifetimeScope registers UiSpawner and GameplayUIFabric.");
            }

            if (_sessionHandler == null)
            {
                throw new InvalidOperationException(
                    $"{name}: GameplaySessionHandler was not injected. Check that GameLifetimeScope registers GameplaySessionHandler and GameplayUIFabric.");
            }

            if (_pauseButton == null)
            {
                throw new InvalidOperationException(
                    $"{name}: PauseButton is not assigned. Drag a Button into the _pauseButton field.");
            }

            if (_pauseMenuPrefab == null)
            {
                throw new InvalidOperationException(
                    $"{name}: PauseMenu prefab is not assigned. Drag the PauseMenu prefab into the _pauseMenuPrefab field.");
            }
        }

        private void OnEnable()
        {
            _pauseButton.onClick.AddListener(OnPauseButtonClick);
        }

        private void OnDisable()
        {
            _pauseButton.onClick.RemoveListener(OnPauseButtonClick);

            if (_activePauseMenu != null)
            {
                _activePauseMenu.CloseButtonClicked -= OnPauseMenuCloseClicked;
                _activePauseMenu.MenuButtonClicked -= OnPauseMenuMenuClicked;
                _activePauseMenu = null;
            }
        }

        private void OnPauseButtonClick()
        {
            if (_activePauseMenu != null)
            {
                return;
            }

            _sessionHandler.PauseByRequest();

            _activePauseMenu = _uiSpawner.Spawn(_pauseMenuPrefab, UiLayer.Popup);
            _activePauseMenu.Initialize(true);
            _activePauseMenu.Closed += OnPauseMenuClosed;
            _uiSpawner.Show(_activePauseMenu);

            _activePauseMenu.CloseButtonClicked += OnPauseMenuCloseClicked;
            _activePauseMenu.MenuButtonClicked += OnPauseMenuMenuClicked;
        }

        private void OnPauseMenuCloseClicked()
        {
            _activePauseMenu.CloseButtonClicked -= OnPauseMenuCloseClicked;
            _activePauseMenu.MenuButtonClicked -= OnPauseMenuMenuClicked;

            _sessionHandler.ResumeByRequest();
            _activePauseMenu.BeginClose();
        }

        private void OnPauseMenuClosed()
        {
            _activePauseMenu.Closed -= OnPauseMenuClosed;
            _uiSpawner.Release(_activePauseMenu);
            _activePauseMenu = null;
        }

        private void OnPauseMenuMenuClicked()
        {
            _activePauseMenu.CloseButtonClicked -= OnPauseMenuCloseClicked;
            _activePauseMenu.MenuButtonClicked -= OnPauseMenuMenuClicked;

            _sessionHandler.ResumeByRequest();
            _activePauseMenu.BeginClose();

            _sessionHandler.ExitToMenu();
        }
    }
}
