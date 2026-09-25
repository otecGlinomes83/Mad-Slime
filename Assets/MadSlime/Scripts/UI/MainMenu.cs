using Audio;
using Cysharp.Threading.Tasks;
using Game;
using Roulette;
using Scriptables;
using System;
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
        [SerializeField] private Button _dailyButton;

        [SerializeField] private PauseMenu _pauseMenu;
        [SerializeField] private LeaderboardMenu _leaderboardMenuPrefab;
        [SerializeField] private RouletteView _dailyRoulette;
        [SerializeField] private SfxClip _musicTrack;

        private MusicPlayer _musicPlayer;
        private Pauser _pauser;
        private GameDirector _gameDirector;
        private IObjectResolver _resolver;
        private bool _isSubscribed;

        [Inject]
        public void Construct(GameDirector gameDirector, Pauser pauser,
            MusicPlayer musicPlayer, IObjectResolver resolver)
        {
            _gameDirector = gameDirector;
            _pauser = pauser;
            _musicPlayer = musicPlayer;
            _resolver = resolver;
        }

        private void Awake()
        {
            if (_gameDirector == null || _pauser == null || _musicPlayer == null || _resolver == null)
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

            if (_dailyButton == null)
            {
                throw new InvalidOperationException(
                    $"{name}: DailyButton is not assigned. Drag a Button into the _dailyButton field.");
            }

            if (_dailyRoulette == null)
            {
                throw new InvalidOperationException(
                    $"{name}: DailyRoulette is not assigned. Drag the daily RouletteView component into the _dailyRoulette field.");
            }
        }

        private void Start()
        {
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
            _dailyButton.onClick.AddListener(OnDailyClicked);
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
            _dailyButton.onClick.RemoveListener(OnDailyClicked);
        }

        private void OnSettingsClicked()
        {
            PauseMenu pauseMenu = _resolver.Instantiate(_pauseMenu);
            pauseMenu.Initialize(false);
        }

        private void OnPlayClicked()
        {
            NavigateTo(SceneId.Game).Forget();
        }

        private void OnShopClicked()
        {
            NavigateTo(SceneId.Shop).Forget();
        }

        private void OnLeaderboardClicked()
        {
            LeaderboardMenu leaderboardMenu = _resolver.Instantiate(_leaderboardMenuPrefab);
            leaderboardMenu.Initialize();
        }

        private void OnDailyClicked()
        {
            _dailyRoulette.Open();
        }

        private async UniTaskVoid NavigateTo(SceneId targetSceneId)
        {
            if (_gameDirector.IsTransitioning == true)
            {
                return;
            }

            await _gameDirector.LoadAsync(targetSceneId);
        }
    }
}
