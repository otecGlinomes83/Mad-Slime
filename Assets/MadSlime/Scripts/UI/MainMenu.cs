using System;
using Audio;
using Game;
using Scriptables;
using UnityEngine;
using UnityEngine.UI;
using VContainer;
using VContainer.Unity;

namespace UI
{
    public sealed class MainMenu : MonoBehaviour
    {
        [SerializeField] private Button _playButton;
        [SerializeField] private Button _shopButton;
        [SerializeField] private Button _leaderboardButton;
        [SerializeField] private Button _settingsButton;

        [SerializeField] private PauseMenu _pauseMenu;
        [SerializeField] private LeaderboardMenu _leaderboardMenuPrefab;
        [SerializeField] private SfxClip _musicTrack;

        private MusicPlayer _musicPlayer;
        private Pauser _pauser;
        private LevelTransitor _levelTransitor;
        private IObjectResolver _resolver;
        private bool _isSubscribed;

        [Inject]
        public void Construct(LevelTransitor levelTransitor, Pauser pauser,
            MusicPlayer musicPlayer, IObjectResolver resolver)
        {
            _levelTransitor = levelTransitor;
            _pauser = pauser;
            _musicPlayer = musicPlayer;
            _resolver = resolver;
        }

        private void Awake()
        {
            if (_levelTransitor == null || _pauser == null || _musicPlayer == null || _resolver == null)
            {
                throw new InvalidOperationException(
                    $"{name}: dependencies were not injected. MenuLifetimeScope must be the first object in the scene hierarchy.");
            }

            if (_musicTrack == null)
            {
                throw new InvalidOperationException(
                    $"{name}: Music track is not assigned. Drag a SfxClip asset into the _musicTrack field.");
            }

            if (_playButton == null)
            {
                throw new InvalidOperationException(
                    $"{name}: PlayButton is not assigned. Drag a Button into the _playButton field.");
            }

            if (_shopButton == null)
            {
                throw new InvalidOperationException(
                    $"{name}: ShopButton is not assigned. Drag a Button into the _shopButton field.");
            }

            if (_leaderboardButton == null)
            {
                throw new InvalidOperationException(
                    $"{name}: LeaderboardButton is not assigned. Drag a Button into the _leaderboardButton field.");
            }

            if (_settingsButton == null)
            {
                throw new InvalidOperationException(
                    $"{name}: SettingsButton is not assigned. Drag a Button into the _settingsButton field.");
            }

            if (_leaderboardMenuPrefab == null)
            {
                throw new InvalidOperationException(
                    $"{name}: LeaderboardMenuPrefab is not assigned. Drag a LeaderboardMenu prefab into the _leaderboardMenuPrefab field.");
            }
        }

        private void Start()
        {
            Time.timeScale = 1f;
            _musicPlayer.Play(_musicTrack);
        }

        private void OnEnable()
        {
            if (_isSubscribed)
            {
                return;
            }

            _isSubscribed = true;

            _playButton.onClick.AddListener(OnPlayClicked);
            _settingsButton.onClick.AddListener(OnSettingsClicked);
            _shopButton.onClick.AddListener(OnShopClicked);
            _leaderboardButton.onClick.AddListener(OnLeaderboardClicked);
        }

        private void OnDisable()
        {
            if (_isSubscribed == false)
            {
                return;
            }

            _isSubscribed = false;

            _playButton.onClick.RemoveListener(OnPlayClicked);
            _settingsButton.onClick.RemoveListener(OnSettingsClicked);
            _shopButton.onClick.RemoveListener(OnShopClicked);
            _leaderboardButton.onClick.RemoveListener(OnLeaderboardClicked);
        }

        private void OnSettingsClicked()
        {
            PauseMenu pauseMenu = _resolver.Instantiate(_pauseMenu);
            pauseMenu.Initialize(false);
        }

        private void OnPlayClicked()
        {
            _levelTransitor.LoadGame();
        }

        private void OnShopClicked()
        {
            _levelTransitor.LoadShop();
        }

        private void OnLeaderboardClicked()
        {
            LeaderboardMenu leaderboardMenu = _resolver.Instantiate(_leaderboardMenuPrefab);
            leaderboardMenu.Initialize();
        }
    }
}