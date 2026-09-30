using Game;
using System;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace UI
{
    public sealed class GameplayUIFabric : MonoBehaviour
    {
        [SerializeField] private Button _pauseButton;
        [SerializeField] private PauseMenu _pauseMenuPrefab;

        private UiSpawner _uiSpawner;
        private GameplaySessionHandler _sessionHandler;

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
        }

        private void OnEnable()
        {
            _pauseButton.onClick.AddListener(SpawnPauseMenu);
        }

        private void OnDisable()
        {
            _pauseButton.onClick.RemoveListener(SpawnPauseMenu);
        }

        private void SpawnPauseMenu()
        {
            PauseMenu pauseMenu = _uiSpawner.Spawn(_pauseMenuPrefab, UiLayer.Popup);
            pauseMenu.Initialize(true, menuAction: _sessionHandler.ExitToMenu);
        }
    }
}
