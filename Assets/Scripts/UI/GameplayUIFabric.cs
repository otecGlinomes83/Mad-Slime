using Audio;
using Game;
using UI;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts.UI
{
    public class GameplayUIFabric : MonoBehaviour
    {
        [SerializeField] private GameplaySessionHandler _sessionHandler;
        [SerializeField] private AudioMixerController _mixerController;

        [SerializeField] private Button _pauseButton;
        [SerializeField] private Button _leaderboardButton;
        [SerializeField] private Button _shopButton;
        [SerializeField] private Button _skinsButton;

        [SerializeField] private PauseMenu _pauseMenuPrefab;
        [SerializeField] private ShopMenu _shopMenuPrefab;
        [SerializeField] private SkinsMenu _skinsMenuPrefab;
        [SerializeField] private LeaderboardMenu _leaderboardMenuPrefab;
        [SerializeField] private DeathMenu _deathMenuPrefab;

        [SerializeField] private Pauser _pauser;

        private void Awake()
        {
            _sessionHandler.GameStarted += HideButtons;
            _pauseButton.onClick.AddListener(SpawnPauseMenu);
            _shopButton.onClick.AddListener(SpawnShopMenu);
            _leaderboardButton.onClick.AddListener(SpawnLeaderboardMenu);
            _skinsButton.onClick.AddListener(SpawnSkinsMenu);

            _sessionHandler.PlayerDied += SpawnDeathMenu;
        }

        private void OnDisable()
        {
            _pauseButton.onClick.RemoveListener(SpawnPauseMenu);
            _shopButton.onClick.RemoveListener(SpawnShopMenu);
            _leaderboardButton.onClick.RemoveListener(SpawnLeaderboardMenu);
            _skinsButton.onClick.RemoveListener(SpawnSkinsMenu);

            _sessionHandler.PlayerDied -= SpawnDeathMenu;
        }

        private void HideButtons()
        {
            _leaderboardButton.gameObject.SetActive(false);
            _skinsButton.gameObject.SetActive(false);
            _shopButton.gameObject.SetActive(false);
        }

        private void SpawnPauseMenu()
        {
            PauseMenu pauseMenu = Instantiate(_pauseMenuPrefab);
            pauseMenu.Initialize(_pauser, _mixerController, showRestart: true, restartAction: _sessionHandler.Restart);
        }

        private void SpawnShopMenu()
        {
            ShopMenu shopMenu = Instantiate(_shopMenuPrefab);
            shopMenu.Initialize(_pauser);
        }

        private void SpawnLeaderboardMenu()
        {
            LeaderboardMenu leaderboardMenu = Instantiate(_leaderboardMenuPrefab);
            leaderboardMenu.Initialize(_pauser);
        }

        private void SpawnSkinsMenu()
        {
            SkinsMenu skinsMenu = Instantiate(_skinsMenuPrefab);
            skinsMenu.Initialize(_pauser);
        }

        private void SpawnDeathMenu()
        {
            DeathMenu deathMenu = Instantiate(_deathMenuPrefab);
            deathMenu.Initialize(_pauser, _sessionHandler.Revive, _sessionHandler.Restart);
        }
    }
}