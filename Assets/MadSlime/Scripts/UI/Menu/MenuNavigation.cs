using System;
using Cysharp.Threading.Tasks;
using Game;
using Roulette;
using Shop;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using VContainer;

namespace UI
{
    public class MenuNavigation : MonoBehaviour
    {
        private enum WindowState
        {
            Stopped,
            Main,
            Skins,
            Upgrades
        }

        [SerializeField] private GameObject _uiRoot;
        [SerializeField] private GameObject _mainWindow;
        [SerializeField] private GameObject _skinsWindow;
        [SerializeField] private GameObject _upgradesWindow;
        [SerializeField] private Camera _previewCamera;
        [SerializeField] private Button _playButton;
        [SerializeField] private Button _skinsButton;
        [SerializeField] private Button _upgradesButton;
        [SerializeField] private Button _leaderboardButton;
        [SerializeField] private Button _settingsButton;
        [SerializeField] private Button _dailyButton;
        [SerializeField] private Button _skinsCloseButton;
        [SerializeField] private Button _upgradesCloseButton;
        [SerializeField] private Button _openSkinButton;
        [SerializeField] private PauseMenu _settingsMenuPrefab;
        [SerializeField] private LeaderboardMenu _leaderboardMenuPrefab;
        [SerializeField] private RoulettePreparation _roulettePreparation;
        [SerializeField] private MenuContent _content;

        private WindowState _state;
        private MenuPopupStack _popups;
        private RoulettePresenter _dailyRoulette;
        private RoulettePresenter _skinRoulette;
        private UiSpawner _spawner;
        private SceneNavigator _sceneNavigator;
        private PauseMenu _settings;
        private LeaderboardMenu _leaderboard;
        private RouletteWinPopup _prize;
        private string _rewardSkinId;
        private bool _isSubscribed;

        public event Action PlayRequested;

        [Inject]
        public void Construct(UiSpawner spawner, SceneNavigator sceneNavigator)
        {
            _spawner = spawner;
            _sceneNavigator = sceneNavigator;
        }

        private void Awake()
        {
            if (_uiRoot == null || _mainWindow == null || _skinsWindow == null
                || _upgradesWindow == null
                || _previewCamera == null || _playButton == null || _skinsButton == null || _upgradesButton == null
                || _leaderboardButton == null || _settingsButton == null || _dailyButton == null
                || _skinsCloseButton == null || _upgradesCloseButton == null || _openSkinButton == null
                || _settingsMenuPrefab == null || _leaderboardMenuPrefab == null || _roulettePreparation == null || _content == null)
            {
                throw new InvalidOperationException($"{name}: assign all menu navigation references.");
            }

            if (_spawner == null || _sceneNavigator == null)
            {
                throw new InvalidOperationException($"{name}: menu navigation dependencies were not injected.");
            }

            _popups = new MenuPopupStack(_mainWindow, _skinsWindow, _upgradesWindow);
        }

        private void OnDisable()
        {
            Stop();
        }

        public void Prepare()
        {
            _uiRoot.SetActive(true);
            Stop();
        }

        public void Run()
        {
            if (_state != WindowState.Stopped)
            {
                return;
            }

            Subscribe();
            SetWindow(WindowState.Main);

            if (_sceneNavigator.TryMarkDailyShown() == true)
            {
                OpenDailyRoulette();
            }
        }

        public void Stop()
        {
            Unsubscribe();
            SetWindow(WindowState.Stopped);
            ReleaseSettings();
            ReleaseLeaderboard();
            ReleasePrize();
            ReleaseDailyRoulette();
            ReleaseSkinRoulette();
            _popups?.Clear();
        }

        private void Subscribe()
        {
            if (_isSubscribed == true)
            {
                return;
            }

            _playButton.onClick.AddListener(OnPlayClicked);
            _skinsButton.onClick.AddListener(OnSkinsClicked);
            _upgradesButton.onClick.AddListener(OnUpgradesClicked);
            _leaderboardButton.onClick.AddListener(OnLeaderboardClicked);
            _settingsButton.onClick.AddListener(OnSettingsClicked);
            _dailyButton.onClick.AddListener(OnDailyClicked);
            _skinsCloseButton.onClick.AddListener(OnCollectionCloseClicked);
            _upgradesCloseButton.onClick.AddListener(OnCollectionCloseClicked);
            _openSkinButton.onClick.AddListener(OnOpenSkinClicked);
            _isSubscribed = true;
        }

        private void Unsubscribe()
        {
            if (_isSubscribed == false)
            {
                return;
            }

            RemoveListener(_playButton, OnPlayClicked);
            RemoveListener(_skinsButton, OnSkinsClicked);
            RemoveListener(_upgradesButton, OnUpgradesClicked);
            RemoveListener(_leaderboardButton, OnLeaderboardClicked);
            RemoveListener(_settingsButton, OnSettingsClicked);
            RemoveListener(_dailyButton, OnDailyClicked);
            RemoveListener(_skinsCloseButton, OnCollectionCloseClicked);
            RemoveListener(_upgradesCloseButton, OnCollectionCloseClicked);
            RemoveListener(_openSkinButton, OnOpenSkinClicked);
            _isSubscribed = false;
        }

        private static void RemoveListener(Button button, UnityAction listener)
        {
            if (button != null)
            {
                button.onClick.RemoveListener(listener);
            }
        }

        private bool CanOpenFromMain()
        {
            return _state == WindowState.Main && CanInteractWithScreen();
        }

        private bool CanInteractWithScreen()
        {
            return _popups.HasOpenPopups() == false && _sceneNavigator.IsTransitioning == false;
        }

        private void OnPlayClicked()
        {
            if (CanOpenFromMain() == true)
            {
                PlayRequested?.Invoke();
            }
        }

        private void OnSkinsClicked()
        {
            if (CanOpenFromMain() == false)
            {
                return;
            }

            SetWindow(WindowState.Skins);
            _content.RefreshSkins(null);
        }

        private void OnUpgradesClicked()
        {
            if (CanOpenFromMain() == true)
            {
                SetWindow(WindowState.Upgrades);
            }
        }

        private void OnCollectionCloseClicked()
        {
            if (CanInteractWithScreen() && (_state == WindowState.Skins || _state == WindowState.Upgrades))
            {
                SetWindow(WindowState.Main);
            }
        }

        private void OnLeaderboardClicked()
        {
            if (CanOpenFromMain() == false)
            {
                return;
            }

            _leaderboard = _spawner.Spawn(_leaderboardMenuPrefab, UiLayer.Popup);
            _leaderboard.CloseRequested += OnLeaderboardCloseRequested;
            _leaderboard.Closed += OnLeaderboardClosed;
            _leaderboard.Initialize();
            _spawner.Show(_leaderboard);
            _popups.Push(_leaderboard);
        }

        private void OnLeaderboardCloseRequested()
        {
            _leaderboard.BeginClose();
        }

        private void OnLeaderboardClosed()
        {
            _popups.Pop(_leaderboard);
            ReleaseLeaderboard();
        }

        private void OnSettingsClicked()
        {
            if (CanOpenFromMain() == false)
            {
                return;
            }

            _settings = _spawner.Spawn(_settingsMenuPrefab, UiLayer.Popup);
            _settings.CloseButtonClicked += OnSettingsCloseRequested;
            _settings.ResetConfirmed += OnResetConfirmed;
            _settings.Closed += OnSettingsClosed;
            _settings.Initialize(false, true);
            _spawner.Show(_settings);
            _popups.Push(_settings);
        }

        private void OnSettingsCloseRequested()
        {
            _settings.BeginClose();
        }

        private void OnResetConfirmed()
        {
            _sceneNavigator.ResetProgressAndLoadMenuAsync().Forget();
        }

        private void OnSettingsClosed()
        {
            _popups.Pop(_settings);
            ReleaseSettings();
        }

        private void OnDailyClicked()
        {
            if (CanOpenFromMain() == true)
            {
                OpenDailyRoulette();
            }
        }

        private void OpenDailyRoulette()
        {
            _dailyRoulette = _roulettePreparation.SpawnDaily();
            _dailyRoulette.CloseRequested += OnDailyCloseRequested;
            _dailyRoulette.Closed += OnDailyClosed;
            _dailyRoulette.Show(OnDailySpinCompleted);
            _popups.Push(_dailyRoulette);
        }

        private void OnDailyCloseRequested()
        {
            if (_popups.IsTop(_dailyRoulette))
            {
                _dailyRoulette.Hide();
            }
        }

        private void OnDailyClosed()
        {
            _popups.Pop(_dailyRoulette);
            ReleaseDailyRoulette();
        }

        private void OnOpenSkinClicked()
        {
            if (_state != WindowState.Skins || CanInteractWithScreen() == false)
            {
                return;
            }

            _roulettePreparation.SkinModel.RefreshSlots();
            if (_roulettePreparation.SkinModel.CanSpin(false) == false)
            {
                return;
            }

            _rewardSkinId = null;
            _skinRoulette = _roulettePreparation.SpawnSkins();
            _skinRoulette.Closed += OnSkinRouletteClosed;
            if (_skinRoulette.ShowAndSpin(OnSkinSpinCompleted))
            {
                _popups.Push(_skinRoulette);
            }
            else
            {
                ReleaseSkinRoulette();
                _content.RefreshSkins(null);
            }
        }

        private void OnSkinRouletteClosed()
        {
            _popups.Pop(_skinRoulette);
            ReleaseSkinRoulette();
            _content.RefreshSkins(_rewardSkinId);
        }

        private void OnDailySpinCompleted(RoulettePrize prize)
        {
            ShowPrize(_dailyRoulette.View, prize);
        }

        private void OnSkinSpinCompleted(RoulettePrize prize)
        {
            if (prize.Skin != null)
            {
                _rewardSkinId = prize.Skin.Id;
            }

            ShowPrize(_skinRoulette.View, prize);
        }

        private void ShowPrize(RouletteView source, RoulettePrize prize)
        {
            _prize = _spawner.Spawn(source.WinPopupPrefab, UiLayer.Popup);

            _prize.Initialize(prize, source);

            _prize.CloseRequested += OnPrizeCloseRequested;
            _prize.Closed += OnPrizeClosed;
            _spawner.Show(_prize);
            _popups.Push(_prize);
        }

        private void OnPrizeCloseRequested()
        {
            if (_popups.IsTop(_prize))
            {
                _prize.BeginClose();
            }
        }

        private void OnPrizeClosed()
        {
            _popups.Pop(_prize);
            ReleasePrize();

            if (_dailyRoulette != null)
            {
                _dailyRoulette.ResumeAfterPrize();
            }
            else if (_skinRoulette != null)
            {
                _skinRoulette.Hide();
            }
        }

        private void SetWindow(WindowState state)
        {
            _state = state;
            SetActiveIfAlive(_mainWindow, state == WindowState.Main);
            SetActiveIfAlive(_skinsWindow, state == WindowState.Skins);
            SetActiveIfAlive(_upgradesWindow, state == WindowState.Upgrades);
            if (_previewCamera != null)
            {
                SetActiveIfAlive(_previewCamera.gameObject, state == WindowState.Skins);
            }
        }

        private static void SetActiveIfAlive(GameObject target, bool active)
        {
            if (target != null)
            {
                target.SetActive(active);
            }
        }

        private void ReleaseDailyRoulette()
        {
            if (_dailyRoulette == null)
            {
                return;
            }

            _dailyRoulette.CloseRequested -= OnDailyCloseRequested;
            _dailyRoulette.Closed -= OnDailyClosed;
            _spawner.Release(_dailyRoulette);
            _dailyRoulette = null;
        }

        private void ReleaseSkinRoulette()
        {
            if (_skinRoulette == null)
            {
                return;
            }

            _skinRoulette.Closed -= OnSkinRouletteClosed;
            _spawner.Release(_skinRoulette);
            _skinRoulette = null;
        }

        private void ReleaseSettings()
        {
            if (_settings == null)
            {
                return;
            }

            _settings.CloseButtonClicked -= OnSettingsCloseRequested;
            _settings.ResetConfirmed -= OnResetConfirmed;
            _settings.Closed -= OnSettingsClosed;
            _spawner.Release(_settings);
            _settings = null;
        }

        private void ReleaseLeaderboard()
        {
            if (_leaderboard == null)
            {
                return;
            }

            _leaderboard.CloseRequested -= OnLeaderboardCloseRequested;
            _leaderboard.Closed -= OnLeaderboardClosed;
            _spawner.Release(_leaderboard);
            _leaderboard = null;
        }

        private void ReleasePrize()
        {
            if (_prize == null)
            {
                return;
            }

            _prize.CloseRequested -= OnPrizeCloseRequested;
            _prize.Closed -= OnPrizeClosed;
            _spawner.Release(_prize);
            _prize = null;
        }
    }
}
