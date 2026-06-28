using Game;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts.UI
{
    public class FillUIFabric : MonoBehaviour
    {
        [SerializeField] private FillSessionHandler _sessionHandler;
        [SerializeField] private Button _pauseButton;

        [SerializeField] private PauseMenu _pauseMenuPrefab;
        [SerializeField] private WinMenu _winMenuPrefab;
        [SerializeField] private FailMenu _failMenuPrefab;

        [SerializeField] private Pauser _pauser;

        private void OnEnable()
        {
            _sessionHandler.Failed += OnGameFailed;
            _sessionHandler.Win += OnGameWin;

            _pauseButton.onClick.AddListener(OnPauseButtonClick);
        }

        private void OnDisable()
        {
            _sessionHandler.Failed -= OnGameFailed;
            _sessionHandler.Win -= OnGameWin;

            _pauseButton.onClick.RemoveListener(OnPauseButtonClick);
        }

        private void OnGameWin()
        {
            WinMenu winMenu = Instantiate(_winMenuPrefab);
            winMenu.Initialize(_pauser, _sessionHandler.LoadNextLevel, _sessionHandler.LoadPreviousLevel);
        }

        private void OnGameFailed()
        {
            FailMenu failMenu = Instantiate(_failMenuPrefab);
            failMenu.Initialize(_pauser, _sessionHandler.LoadNextLevel, _sessionHandler.LoadPreviousLevel);
        }

        private void OnPauseButtonClick()
        {
            PauseMenu pauseMenu = Instantiate(_pauseMenuPrefab);
            pauseMenu.Initialize(_pauser);
        }
    }
}