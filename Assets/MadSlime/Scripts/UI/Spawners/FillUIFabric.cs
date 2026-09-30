using Game;
using Scriptables;
using System;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace UI
{
    public sealed class FillUIFabric : MonoBehaviour
    {
        [SerializeField] private Button _pauseButton;

        [SerializeField] private PauseMenu _pauseMenuPrefab;
        [SerializeField] private WinMenu _winMenuPrefab;
        [SerializeField] private FailMenu _failMenuPrefab;

        [SerializeField] private YandexConfig _yandexConfig;

        private UiSpawner _uiSpawner;
        private FillSessionHandler _sessionHandler;
        private Wallet _wallet;
        private AdScheduler _adScheduler;
        private FailMenu _activeFailMenu;
        private int _lastRewardAmount;

        [Inject]
        public void Construct(UiSpawner uiSpawner, FillSessionHandler sessionHandler, Wallet wallet,
            AdScheduler adScheduler)
        {
            _uiSpawner = uiSpawner;
            _sessionHandler = sessionHandler;
            _wallet = wallet;
            _adScheduler = adScheduler;
        }

        private void Awake()
        {
            if (_uiSpawner == null)
            {
                throw new InvalidOperationException(
                    $"{name}: UiSpawner was not injected. Check that FillLifetimeScope registers UiSpawner and FillUIFabric.");
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

            if (_pauseMenuPrefab == null || _winMenuPrefab == null || _failMenuPrefab == null)
            {
                throw new InvalidOperationException(
                    $"{name}: a window prefab is not assigned. Drag the PauseMenu, WinMenu and FailMenu prefabs into the fields.");
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

            WinMenu winMenu = _uiSpawner.Spawn(_winMenuPrefab, UiLayer.Popup);
            winMenu.Initialize(
                rewardAmount,
                _sessionHandler.LoadNextLevel,
                RequestDoubleReward,
                _sessionHandler.ExitToMenuAfterWin);
        }

        private void OnGameFailed(int rewardAmount)
        {
            _activeFailMenu = _uiSpawner.Spawn(_failMenuPrefab, UiLayer.Popup, OnFailMenuClosed);
            _activeFailMenu.Initialize(
                rewardAmount,
                RequestFillRescue,
                _sessionHandler.CanRescueFill,
                OnRestartFromFail,
                _sessionHandler.ExitToMenu);
        }

        private void OnFailMenuClosed()
        {
            _activeFailMenu = null;
        }

        private void RequestFillRescue()
        {
            _adScheduler.ShowRewarded(
                _yandexConfig.FillRescueRewardId,
                OnFillRescueGranted,
                OnFillRescueRejected);
        }

        private void OnFillRescueGranted()
        {
            if (_activeFailMenu != null)
            {
                _activeFailMenu.Dismiss();
            }

            _sessionHandler.RescueFill();
        }

        private void OnFillRescueRejected()
        {
            if (_activeFailMenu != null)
            {
                _activeFailMenu.OnRescueRejected();
            }
        }

        private void OnRestartFromFail()
        {
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
            PauseMenu pauseMenu = _uiSpawner.Spawn(_pauseMenuPrefab, UiLayer.Popup);
            pauseMenu.Initialize(false);
        }
    }
}
