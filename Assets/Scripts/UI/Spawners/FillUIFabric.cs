using Audio;
using Game;
using Scriptables;
using System;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public sealed class FillUIFabric : MonoBehaviour
    {
        [SerializeField] private FillSessionHandler _sessionHandler;
        [SerializeField] private AudioMixerController _mixerController;

        [SerializeField] private Button _pauseButton;

        [SerializeField] private PauseMenu _pauseMenuPrefab;
        [SerializeField] private WinMenu _winMenuPrefab;
        [SerializeField] private FailMenu _failMenuPrefab;

        [SerializeField] private Wallet _wallet;
        [SerializeField] private AdScheduler _adScheduler;
        [SerializeField] private YandexConfig _yandexConfig;

        [SerializeField] private Pauser _pauser;

        private int _lastRewardAmount;

        private void Awake()
        {
            if (_sessionHandler == null)
            {
                throw new InvalidOperationException(
                    $"{name}: FillSessionHandler is not assigned. Drag a FillSessionHandler into the _sessionHandler field.");
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

            WinMenu winMenu = Instantiate(_winMenuPrefab);
            winMenu.Initialize(
                rewardAmount,
                _pauser,
                _sessionHandler.LoadNextLevel,
                RequestDoubleReward);
        }

        private void OnGameFailed(int rewardAmount)
        {
            FailMenu failMenu = Instantiate(_failMenuPrefab);
            failMenu.Initialize(
                rewardAmount,
                _pauser,
                RequestNextLevelForRewarded,
                OnRestartFromFail);
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
            PauseMenu pauseMenu = Instantiate(_pauseMenuPrefab);
            pauseMenu.Initialize(_pauser, _mixerController, showRestart: false, restartAction: null);
        }
    }
}
