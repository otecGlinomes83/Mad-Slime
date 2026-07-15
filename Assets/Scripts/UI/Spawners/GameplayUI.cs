using Audio;
using Game;
using Skins;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class GameplayUIFabric : MonoBehaviour
    {
        [SerializeField] private GameplaySessionHandler _sessionHandler;
        [SerializeField] private AudioMixerController _mixerController;

        [SerializeField] private Button _pauseButton;
        [SerializeField] private Button _leaderboardButton;
        [SerializeField] private Button _shopButton;

        [SerializeField] private PauseMenu _pauseMenuPrefab;
        [SerializeField] private LeaderboardMenu _leaderboardMenuPrefab;
        [SerializeField] private DeathMenu _deathMenuPrefab;

        [SerializeField] private Pauser _pauser;

        [SerializeField] private LevelTransitor _levelTransitor;

        private void Awake()
        {
            _sessionHandler.GameStarted += HideButtons;
            _pauseButton.onClick.AddListener(SpawnPauseMenu);
            _leaderboardButton.onClick.AddListener(SpawnLeaderboardMenu);
            _shopButton.onClick.AddListener(SpawnShop);

            _sessionHandler.PlayerDied += SpawnDeathMenu;
        }

        private void OnDisable()
        {
            _pauseButton.onClick.RemoveListener(SpawnPauseMenu);
            _leaderboardButton.onClick.RemoveListener(SpawnLeaderboardMenu);
            _shopButton.onClick.RemoveListener(SpawnShop);

            _sessionHandler.PlayerDied -= SpawnDeathMenu;
        }

        private void HideButtons()
        {
            _leaderboardButton.gameObject.SetActive(false);
            _shopButton.gameObject.SetActive(false);
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

        private void SpawnShop()
        {
            _levelTransitor.LoadShop();
        }

        private void SpawnDeathMenu()
        {
            DeathMenu deathMenu = Instantiate(_deathMenuPrefab);
            deathMenu.Initialize(_pauser, _sessionHandler.Revive, _sessionHandler.Restart);
        }
    }
}