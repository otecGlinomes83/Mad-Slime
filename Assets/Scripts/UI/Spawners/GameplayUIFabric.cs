using Audio;
using Game;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class GameplayUIFabric : MonoBehaviour
    {
        [SerializeField] private GameplaySessionHandler _sessionHandler;
        [SerializeField] private AudioMixerController _mixerController;

        [SerializeField] private GameObject _buttonsCanvas;

        [SerializeField] private Button _pauseButton;
        [SerializeField] private Button _leaderboardButton;
        [SerializeField] private Button _shopButton;

        [SerializeField] private PauseMenu _pauseMenuPrefab;
        [SerializeField] private LeaderboardMenu _leaderboardMenuPrefab;

        [SerializeField] private Pauser _pauser;

        [SerializeField] private LevelTransitor _levelTransitor;

        private void Awake()
        {
            _sessionHandler.GameStarted += HideButtons;

            _pauseButton.onClick.AddListener(SpawnPauseMenu);
            _leaderboardButton.onClick.AddListener(SpawnLeaderboardMenu);
            _shopButton.onClick.AddListener(LoadShop);
        }

        private void OnDisable()
        {
            _sessionHandler.GameStarted -= HideButtons;

            _pauseButton.onClick.RemoveListener(SpawnPauseMenu);
            _leaderboardButton.onClick.RemoveListener(SpawnLeaderboardMenu);
            _shopButton.onClick.RemoveListener(LoadShop);
        }

        private void HideButtons()
        {
            _buttonsCanvas.SetActive(false);
        }

        private void SpawnPauseMenu()
        {
            PauseMenu pauseMenu = Instantiate(_pauseMenuPrefab);
            pauseMenu.Initialize(_pauser, _mixerController, showRestart: true, restartAction: _sessionHandler.Restart);
        }

        private void SpawnLeaderboardMenu()
        {
            LeaderboardMenu leaderboardMenu = Instantiate(_leaderboardMenuPrefab);
            leaderboardMenu.Initialize(_pauser);
        }

        private void LoadShop()
        {
            _levelTransitor.LoadShop();
        }
    }
}
