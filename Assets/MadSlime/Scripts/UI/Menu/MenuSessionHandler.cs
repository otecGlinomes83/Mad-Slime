using Audio;
using Cysharp.Threading.Tasks;
using Game;
using Roulette;
using Scriptables;
using Saves;
using Shop;
using Skins;
using System;
using System.Threading;
using UnityEngine;
using VContainer;

namespace UI
{
    public class MenuSessionHandler : MonoBehaviour
    {
        [SerializeField] private MainMenu _mainMenu;
        [SerializeField] private Playlist _musicPlaylist;
        [SerializeField] private PauseMenu _settingsMenuPrefab;
        [SerializeField] private LeaderboardMenu _leaderboardMenuPrefab;
        [SerializeField] private RoulettePresenter _dailyRoulette;

        private enum SessionState
        {
            Preparing,
            Running,
            Finished
        }

        private SessionState _state;
        private CancellationTokenSource _sessionSource;
        private IMusicPlayer _musicPlayer;
        private SceneNavigator _sceneNavigator;
        private UiSpawner _uiSpawner;
        private Pauser _pauser;
        private PauseMenu _activeSettingsMenu;
        private LeaderboardMenu _activeLeaderboard;
        private ISaveConfirmation _saveConfirmation;
        private RouletteFactory _rouletteFactory;
        private AdScheduler _adScheduler;
        private IUISoundPlayer _soundPlayer;
        private ShopContent _shopContent;
        private RouletteConfig _rouletteConfig;
        private ILevelStorage _levelStorage;
        private LevelLabelUI _levelLabel;

        [Inject]
        public void Construct(IMusicPlayer musicPlayer, SceneNavigator sceneNavigator, UiSpawner uiSpawner,
            Pauser pauser, ILevelStorage levelStorage, LevelLabelUI levelLabel, RouletteFactory rouletteFactory,
            AdScheduler adScheduler, IUISoundPlayer soundPlayer, ShopContent shopContent, RouletteConfig rouletteConfig, ISaveConfirmation saveConfirmation)
        {
            _musicPlayer = musicPlayer;
            _sceneNavigator = sceneNavigator;
            _uiSpawner = uiSpawner;
            _pauser = pauser;
            _levelStorage = levelStorage;
            _levelLabel = levelLabel;
            _rouletteFactory = rouletteFactory;
            _adScheduler = adScheduler;
            _soundPlayer = soundPlayer;
            _shopContent = shopContent;
            _rouletteConfig = rouletteConfig;
            _saveConfirmation = saveConfirmation;
        }

        private void Awake()
        {
            if (_mainMenu == null)
            {
                throw new InvalidOperationException(
                    $"{name}: MainMenu is not assigned. Drag the MainMenu component into the _mainMenu field.");
            }

            if (_musicPlaylist == null)
            {
                throw new InvalidOperationException(
                    $"{name}: Music playlist is not assigned. Drag a Playlist asset into the _musicPlaylist field.");
            }

            if (_settingsMenuPrefab == null)
            {
                throw new InvalidOperationException(
                    $"{name}: Settings menu prefab is not assigned. Drag the PauseMenu prefab into the _settingsMenuPrefab field.");
            }

            if (_leaderboardMenuPrefab == null)
            {
                throw new InvalidOperationException(
                    $"{name}: Leaderboard menu prefab is not assigned. Drag the LeaderboardMenu prefab into the _leaderboardMenuPrefab field.");
            }

            if (_dailyRoulette == null)
            {
                throw new InvalidOperationException(
                    $"{name}: Daily roulette is not assigned. Drag the daily RouletteView component into the _dailyRoulette field.");
            }

            if (_musicPlayer == null || _sceneNavigator == null || _uiSpawner == null || _pauser == null)
            {
                throw new InvalidOperationException(
                    $"{name}: dependencies were not injected. MenuLifetimeScope must be the first object in the scene hierarchy.");
            }
        }

        private void OnEnable()
        {
            _sessionSource = CancellationTokenSource.CreateLinkedTokenSource(this.GetCancellationTokenOnDestroy());
            _mainMenu.PlayClicked += OnPlayClicked;
            _mainMenu.ShopClicked += OnShopClicked;
            _mainMenu.LeaderboardClicked += OnLeaderboardClicked;
            _mainMenu.SettingsClicked += OnSettingsClicked;
            _mainMenu.DailyClicked += OnDailyClicked;

        }

        private void OnDisable()
        {
            FinishSession();
            _sceneNavigator.UnregisterSession(RunSession);
            _sessionSource.Dispose();
            _sessionSource = null;
            if (_mainMenu != null)
            {
                _mainMenu.PlayClicked -= OnPlayClicked;
                _mainMenu.ShopClicked -= OnShopClicked;
                _mainMenu.LeaderboardClicked -= OnLeaderboardClicked;
                _mainMenu.SettingsClicked -= OnSettingsClicked;
                _mainMenu.DailyClicked -= OnDailyClicked;
            }
        }

        private void Start()
        {
            _sceneNavigator.RegisterSession(PrepareSessionAsync, RunSession, FinishSession, _sessionSource.Token);
        }

        private UniTask PrepareSessionAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            int sectorCount = _dailyRoulette.View.Wheel.SectorCount;
            new RouletteConfigValidator().Validate(_rouletteConfig, _shopContent.SkinItems, sectorCount, sectorCount);
            _dailyRoulette.Setup(_rouletteFactory.CreateDaily(sectorCount), _adScheduler, _soundPlayer, _uiSpawner, _saveConfirmation);
            _levelLabel.SetLevel(_levelStorage.CurrentLevel);
            return UniTask.CompletedTask;
        }

        private void RunSession()
        {
            _state = SessionState.Running;
            _musicPlayer.Play(_musicPlaylist);

            if (_sceneNavigator.TryMarkDailyShown())
            {
                ShowDailyRoulette();
            }
        }

        private void FinishSession()
        {
            if (_state == SessionState.Finished)
            {
                return;
            }

            _state = SessionState.Finished;
            _sessionSource?.Cancel();
            _dailyRoulette.Hide();

            if (_activeLeaderboard != null)
            {
                ReleaseLeaderboard();
            }

            if (_activeSettingsMenu != null)
            {
                ReleaseSettings();
            }
        }

        private void OnPlayClicked()
        {
            if (CanNavigate() == false)
            {
                return;
            }

            _dailyRoulette.Hide();
            _pauser.ResetToPlay();
            NavigateToGame().Forget();
        }

        private void OnShopClicked()
        {
            if (CanNavigate() == false)
            {
                return;
            }

            _dailyRoulette.Hide();
            _pauser.ResetToPlay();
            NavigateToShop().Forget();
        }

        private void OnLeaderboardClicked()
        {
            if (CanNavigate() == false || _activeLeaderboard != null)
            {
                return;
            }

            _activeLeaderboard = _uiSpawner.Spawn(_leaderboardMenuPrefab, UiLayer.Popup);
            _activeLeaderboard.Initialize();
            _activeLeaderboard.CloseRequested += OnLeaderboardCloseRequested;
            _activeLeaderboard.Closed += OnLeaderboardClosed;
            _uiSpawner.Show(_activeLeaderboard);
        }

        private void OnLeaderboardCloseRequested()
        {
            _activeLeaderboard.BeginClose();
        }

        private void OnLeaderboardClosed()
        {
            ReleaseLeaderboard();
        }

        private void ReleaseLeaderboard()
        {
            _activeLeaderboard.CloseRequested -= OnLeaderboardCloseRequested;
            _activeLeaderboard.Closed -= OnLeaderboardClosed;
            _uiSpawner.Release(_activeLeaderboard);
            _activeLeaderboard = null;
        }

        private void OnSettingsClicked()
        {
            if (CanNavigate() == false || _activeSettingsMenu != null)
            {
                return;
            }

            _activeSettingsMenu = _uiSpawner.Spawn(_settingsMenuPrefab, UiLayer.Popup);

            _activeSettingsMenu.Initialize(false);
            _uiSpawner.Show(_activeSettingsMenu);

            _activeSettingsMenu.CloseButtonClicked += OnSettingsCloseClicked;
            _activeSettingsMenu.Closed += OnSettingsClosed;
        }

        private void OnSettingsCloseClicked()
        {
            _activeSettingsMenu.CloseButtonClicked -= OnSettingsCloseClicked;

            _activeSettingsMenu.BeginClose();
        }

        private void OnSettingsClosed()
        {
            ReleaseSettings();
        }

        private void ReleaseSettings()
        {
            _activeSettingsMenu.CloseButtonClicked -= OnSettingsCloseClicked;
            _activeSettingsMenu.Closed -= OnSettingsClosed;
            _uiSpawner.Release(_activeSettingsMenu);
            _activeSettingsMenu = null;
        }

        private void OnDailyClicked()
        {
            if (CanNavigate() == false)
            {
                return;
            }

            ShowDailyRoulette();
        }

        private void ShowDailyRoulette()
        {
            _dailyRoulette.Show();
        }

        private bool CanNavigate()
        {
            return _state == SessionState.Running && _sceneNavigator.IsTransitioning == false;
        }

        private async UniTaskVoid NavigateToGame()
        {
            await _sceneNavigator.LoadGameAsync();
        }

        private async UniTaskVoid NavigateToShop()
        {
            await _sceneNavigator.LoadShopAsync();
        }
    }
}
