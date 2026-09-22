using Game;
using Scriptables;
using System;
using UnityEngine;
using UnityEngine.UI;
using VContainer;
using VContainer.Unity;

namespace UI
{
    public sealed class FillUIFabric : MonoBehaviour
    {
        [SerializeField] private Button _pauseButton;

        [SerializeField] private PauseMenu _pauseMenuPrefab;
        [SerializeField] private WinMenu _winMenuPrefab;
        [SerializeField] private FailMenu _failMenuPrefab;

        [SerializeField] private YandexConfig _yandexConfig;

        private IObjectResolver _resolver;
        private FillSessionHandler _sessionHandler;
        private Wallet _wallet;
        private AdScheduler _adScheduler;
        private int _lastRewardAmount;

        [Inject]
        public void Construct(IObjectResolver resolver, FillSessionHandler sessionHandler, Wallet wallet,
            AdScheduler adScheduler)
        {
            _resolver = resolver;
            _sessionHandler = sessionHandler;
            _wallet = wallet;
            _adScheduler = adScheduler;
        }

        private void Awake()
        {
            if (_resolver == null)
            {
                throw new InvalidOperationException(
                    $"{name}: Resolver was not injected. FillLifetimeScope must be the first object in the scene hierarchy.");
            }

            if (_sessionHandler == null)
            {
                throw new InvalidOperationException(
                    $"{name}: FillSessionHandler was not injected. Check that FillLifetimeScope registers FillSessionHandler and FillUIFabric.");
            }

            if (_wallet == null)
            {
                throw new InvalidOperationException(
                    $"{name}: Wallet was not injected. Check that FillLifetimeScope registers Wallet and FillUIFabric.");
            }

            if (_adScheduler == null)
            {
                throw new InvalidOperationException(
                    $"{name}: AdScheduler was not injected. Check that FillLifetimeScope registers AdScheduler and FillUIFabric.");
            }

            if (_pauseButton == null)
            {
                throw new InvalidOperationException(
                    $"{name}: PauseButton is not assigned. Drag a Button into the _pauseButton field.");
            }
        }

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

        private void OnGameWin(int rewardAmount)
        {
            _lastRewardAmount = rewardAmount;

            WinMenu winMenu = _resolver.Instantiate(_winMenuPrefab);
            winMenu.Initialize(
                rewardAmount,
                _sessionHandler.LoadNextLevel,
                RequestDoubleReward,
                _sessionHandler.ExitToMenuAfterWin);
        }

        private void OnGameFailed(int rewardAmount)
        {
            FailMenu failMenu = _resolver.Instantiate(_failMenuPrefab);
            failMenu.Initialize(
                rewardAmount,
                RequestNextLevelForRewarded,
                OnRestartFromFail,
                _sessionHandler.ExitToMenu);
        }

        private void RequestNextLevelForRewarded()
        {
            _adScheduler.ShowRewarded(
                _yandexConfig.NextLevelRewardId,
                _sessionHandler.LoadNextLevel);
        }

        private void OnRestartFromFail()
        {
            _adScheduler.TryShowInterstitial();
            _sessionHandler.RestartLevel();
        }

        private void RequestDoubleReward()
        {
            _adScheduler.ShowDoubleReward(OnDoubleRewardGranted);
        }

        private void OnDoubleRewardGranted()
        {
            _wallet.Add(_lastRewardAmount);
        }

        private void OnPauseButtonClick()
        {
            PauseMenu pauseMenu = _resolver.Instantiate(_pauseMenuPrefab);
            pauseMenu.Initialize(false);
        }
    }
}