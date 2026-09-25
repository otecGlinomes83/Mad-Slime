using Game;
using System;
using UnityEngine;
using UnityEngine.UI;
using VContainer;
using VContainer.Unity;

namespace UI
{
    public sealed class GameplayUIFabric : MonoBehaviour
    {
        [SerializeField] private Button _pauseButton;
        [SerializeField] private PauseMenu _pauseMenuPrefab;

        private IObjectResolver _resolver;
        private GameplaySessionHandler _sessionHandler;

        [Inject]
        public void Construct(IObjectResolver resolver, GameplaySessionHandler sessionHandler)
        {
            _resolver = resolver;
            _sessionHandler = sessionHandler;
        }

        private void Awake()
        {
            if (_resolver == null)
            {
                throw new InvalidOperationException(
                    $"{name}: Resolver was not injected. GameLifetimeScope must be the first object in the scene hierarchy.");
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
            PauseMenu pauseMenu = _resolver.Instantiate(_pauseMenuPrefab);
            pauseMenu.Initialize(true, menuAction: _sessionHandler.ExitToMenu);
        }
    }
}