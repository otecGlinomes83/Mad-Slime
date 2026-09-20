using Audio;
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
        [SerializeField] private GameplaySessionHandler _sessionHandler;
        [SerializeField] private AudioMixerController _mixerController;

        [SerializeField] private Button _pauseButton;

        [SerializeField] private PauseMenu _pauseMenuPrefab;

        [SerializeField] private Pauser _pauser;

        private IObjectResolver _resolver;

        [Inject]
        public void Construct(IObjectResolver resolver)
        {
            _resolver = resolver;
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
                    $"{name}: GameplaySessionHandler is not assigned. Drag a GameplaySessionHandler into the _sessionHandler field.");
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
            pauseMenu.Initialize(
                _pauser,
                _mixerController, true,
                menuAction: _sessionHandler.ExitToMenu);
        }
    }
}