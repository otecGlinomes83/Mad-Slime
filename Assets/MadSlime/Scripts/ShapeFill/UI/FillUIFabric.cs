using Cysharp.Threading.Tasks;
using Game;
using Saves;
using Scriptables;
using System;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace UI
{
    public class FillUIFabric : MonoBehaviour
    {
        private enum WindowAction
        {
            None,
            NextLevel,
            Restart,
            Menu,
            Rescue
        }

        [SerializeField] private Button _pauseButton;
        [SerializeField] private PauseMenu _pauseMenuPrefab;
        [SerializeField] private WinMenu _winMenuPrefab;
        [SerializeField] private FailMenu _failMenuPrefab;
        [SerializeField] private YandexConfig _yandexConfig;

        private UiSpawner _uiSpawner;
        private FillSessionHandler _sessionHandler;
        private Wallet _wallet;
        private AdScheduler _adScheduler;
        private ISaveConfirmation _saveConfirmation;
        private WinMenu _activeWinMenu;
        private FailMenu _activeFailMenu;
        private PauseMenu _activePauseMenu;
        private CancellationTokenSource _windowCancellation;
        private WindowAction _pendingAction;
        private bool _isActive;
        private bool _isClosingWindow;
        private bool _hasPauseRequest;
        private bool _isDoubleRewardRequested;
        private bool _isDoubleRewardGranted;
        private bool _isRescueResolved;
        private bool _isRescueRequestRunning;
        private int _lastRewardAmount;
        private int _lastFailRewardAmount;

        [Inject]
        public void Construct(UiSpawner uiSpawner, FillSessionHandler sessionHandler, Wallet wallet,
            AdScheduler adScheduler, ISaveConfirmation saveConfirmation)
        {
            _uiSpawner = uiSpawner;
            _sessionHandler = sessionHandler;
            _wallet = wallet;
            _adScheduler = adScheduler;
            _saveConfirmation = saveConfirmation;
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

            if (_yandexConfig == null)
            {
                throw new InvalidOperationException(
                    $"{name}: YandexConfig is not assigned. Drag the YandexConfig asset into the _yandexConfig field.");
            }
        }

        private void OnEnable()
        {
            if (_sessionHandler.IsFinished == true)
            {
                return;
            }

            _isActive = true;
            _windowCancellation = CancellationTokenSource.CreateLinkedTokenSource(this.GetCancellationTokenOnDestroy());
            _sessionHandler.Failed += OnGameFailed;
            _sessionHandler.Win += OnGameWin;
            _sessionHandler.Finished += OnSessionFinished;
            _pauseButton.onClick.AddListener(OnPauseButtonClick);
        }

        private void OnDisable()
        {
            _sessionHandler.Failed -= OnGameFailed;
            _sessionHandler.Win -= OnGameWin;
            _sessionHandler.Finished -= OnSessionFinished;
            _pauseButton.onClick.RemoveListener(OnPauseButtonClick);
            OnSessionFinished();
        }

        private void OnSessionFinished()
        {
            _isActive = false;
            _isDoubleRewardRequested = false;
            _isRescueRequestRunning = false;
            _pendingAction = WindowAction.None;

            if (_windowCancellation != null)
            {
                _windowCancellation.Cancel();
                _windowCancellation.Dispose();
                _windowCancellation = null;
            }

            ReleaseWinMenu();
            ReleaseFailMenu();
            ReleasePauseMenu();
            ResumeOwnedPause();
        }

        private void OnGameWin(int rewardAmount)
        {
            if (_isActive == false || _activeWinMenu != null)
            {
                return;
            }

            _lastRewardAmount = rewardAmount;
            _isDoubleRewardRequested = false;
            _isDoubleRewardGranted = false;
            _isClosingWindow = false;
            _activeWinMenu = _uiSpawner.Spawn(_winMenuPrefab, UiLayer.Popup);
            _activeWinMenu.Initialize(rewardAmount, _lastFailRewardAmount, true);
            _activeWinMenu.NextLevelRequested += OnWinNextLevelRequested;
            _activeWinMenu.DoubleRewardRequested += OnWinDoubleRewardRequested;
            _activeWinMenu.MenuRequested += OnWinMenuRequested;
            _activeWinMenu.Closed += OnWinMenuClosed;
            _uiSpawner.Show(_activeWinMenu);
            _activeWinMenu.PlayRewardCountUp(rewardAmount);
        }

        private void OnGameFailed(int rewardAmount)
        {
            if (_isActive == false || _activeFailMenu != null)
            {
                return;
            }

            _lastFailRewardAmount = rewardAmount;
            _isRescueRequestRunning = false;
            _isRescueResolved = false;
            _isClosingWindow = false;
            _activeFailMenu = _uiSpawner.Spawn(_failMenuPrefab, UiLayer.Popup);
            _activeFailMenu.Initialize(rewardAmount, _sessionHandler.CanRescueFill);
            _activeFailMenu.RescueRequested += OnFailRescueRequested;
            _activeFailMenu.RestartRequested += OnFailRestartRequested;
            _activeFailMenu.MenuRequested += OnFailMenuRequested;
            _activeFailMenu.Closed += OnFailMenuClosed;
            _uiSpawner.Show(_activeFailMenu);
        }

        private void OnFailRescueRequested()
        {
            if (_isActive == false || _isClosingWindow == true || _activeFailMenu == null
                || _isRescueRequestRunning == true || _isRescueResolved == true)
            {
                return;
            }

            _isRescueRequestRunning = true;
            _activeFailMenu.LockRescue();
            _adScheduler.ShowRewarded(_yandexConfig.FillRescueRewardId, OnRescueAdGranted,
                OnRescueAdCompletedWithoutReward, OnRescueAdRejected, OnRescueAdError);
        }

        private void OnRescueAdGranted()
        {
            ResolveRescue();
        }

        private void OnRescueAdError()
        {
            ResolveRescue();
        }

        private void ResolveRescue()
        {
            if (_isActive == false || _isClosingWindow == true || _isRescueResolved == true
                || _isRescueRequestRunning == false || _activeFailMenu == null)
            {
                return;
            }

            _isRescueRequestRunning = false;
            _isRescueResolved = true;
            CloseFailMenu(WindowAction.Rescue);
        }

        private void OnRescueAdCompletedWithoutReward()
        {
            if (_isActive == true && _isClosingWindow == false)
            {
                _isRescueRequestRunning = false;
            }
        }

        private void OnRescueAdRejected()
        {
            if (_isActive == true && _isClosingWindow == false)
            {
                _isRescueRequestRunning = false;
            }
        }

        private void CloseFailMenu(WindowAction action)
        {
            if (_isActive == false || _activeFailMenu == null || _isClosingWindow == true)
            {
                return;
            }

            _isClosingWindow = true;
            _isRescueRequestRunning = false;
            _pendingAction = action;
            UnsubscribeFailRequests();
            _activeFailMenu.BeginClose();
        }

        private void OnFailMenuClosed()
        {
            ReleaseFailMenu();
            CompleteWindowAction();
        }

        private void OnFailRestartRequested()
        {
            CloseFailMenu(WindowAction.Restart);
        }

        private void OnFailMenuRequested()
        {
            CloseFailMenu(WindowAction.Menu);
        }

        private void CloseWinMenu(WindowAction action)
        {
            if (_isActive == false || _activeWinMenu == null || _isClosingWindow == true)
            {
                return;
            }

            _isClosingWindow = true;
            _isDoubleRewardRequested = false;
            _pendingAction = action;
            UnsubscribeWinRequests();
            _activeWinMenu.BeginClose();
        }

        private void OnWinMenuClosed()
        {
            ReleaseWinMenu();
            CompleteWindowAction();
        }

        private void OnWinNextLevelRequested()
        {
            CloseWinMenu(WindowAction.NextLevel);
        }

        private void OnWinMenuRequested()
        {
            CloseWinMenu(WindowAction.Menu);
        }

        private void OnWinDoubleRewardRequested()
        {
            if (_isActive == false || _isClosingWindow == true || _activeWinMenu == null
                || _isDoubleRewardRequested == true || _isDoubleRewardGranted == true)
            {
                return;
            }

            _isDoubleRewardRequested = true;
            _activeWinMenu.SetDoubleRewardAvailable(false);
            _adScheduler.ShowDoubleReward(OnDoubleRewardGranted);
        }

        private void OnDoubleRewardGranted()
        {
            if (_isActive == false || _isClosingWindow == true || _activeWinMenu == null
                || _isDoubleRewardGranted == true || _isDoubleRewardRequested == false)
            {
                return;
            }

            _isDoubleRewardGranted = true;
            _isDoubleRewardRequested = false;
            _wallet.Add(_lastRewardAmount);
            ShowDoubleRewardAsync(_windowCancellation.Token).Forget();
        }

        private async UniTaskVoid ShowDoubleRewardAsync(CancellationToken cancellationToken)
        {
            try
            {
                await _saveConfirmation.ConfirmSavedAsync(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            if (_isActive == true && _isClosingWindow == false && _activeWinMenu != null)
            {
                _activeWinMenu.PlayRewardCountUp(checked(_lastRewardAmount * 2));
            }
        }

        private void OnPauseButtonClick()
        {
            if (_isActive == false || _activePauseMenu != null || _activeWinMenu != null || _activeFailMenu != null)
            {
                return;
            }

            _hasPauseRequest = true;
            _sessionHandler.PauseByRequest();
            _activePauseMenu = _uiSpawner.Spawn(_pauseMenuPrefab, UiLayer.Popup);
            _activePauseMenu.Initialize(false);
            _activePauseMenu.CloseButtonClicked += OnPauseMenuCloseClicked;
            _activePauseMenu.MenuButtonClicked += OnPauseMenuMenuClicked;
            _activePauseMenu.Closed += OnPauseMenuClosed;
            _uiSpawner.Show(_activePauseMenu);
        }

        private void OnPauseMenuCloseClicked()
        {
            ClosePauseMenu(WindowAction.None);
        }

        private void OnPauseMenuMenuClicked()
        {
            ClosePauseMenu(WindowAction.Menu);
        }

        private void ClosePauseMenu(WindowAction action)
        {
            if (_activePauseMenu == null || _isClosingWindow == true)
            {
                return;
            }

            _isClosingWindow = true;
            _pendingAction = action;
            UnsubscribePauseRequests();
            _activePauseMenu.BeginClose();
        }

        private void OnPauseMenuClosed()
        {
            ReleasePauseMenu();
            ResumeOwnedPause();
            CompleteWindowAction();
        }

        private void ResumeOwnedPause()
        {
            if (_hasPauseRequest == false)
            {
                return;
            }

            _hasPauseRequest = false;
            _sessionHandler.ResumeByRequest();
        }

        private void CompleteWindowAction()
        {
            WindowAction action = _pendingAction;
            _pendingAction = WindowAction.None;
            _isClosingWindow = false;

            if (_isActive == false)
            {
                return;
            }

            switch (action)
            {
                case WindowAction.NextLevel:
                    _sessionHandler.LoadNextLevel();
                    break;
                case WindowAction.Restart:
                    _sessionHandler.RestartLevel();
                    break;
                case WindowAction.Menu:
                    _sessionHandler.ExitToMenu();
                    break;
                case WindowAction.Rescue:
                    _sessionHandler.RescueFill();
                    break;
            }
        }

        private void UnsubscribeWinRequests()
        {
            _activeWinMenu.NextLevelRequested -= OnWinNextLevelRequested;
            _activeWinMenu.DoubleRewardRequested -= OnWinDoubleRewardRequested;
            _activeWinMenu.MenuRequested -= OnWinMenuRequested;
        }

        private void UnsubscribeFailRequests()
        {
            _activeFailMenu.RescueRequested -= OnFailRescueRequested;
            _activeFailMenu.RestartRequested -= OnFailRestartRequested;
            _activeFailMenu.MenuRequested -= OnFailMenuRequested;
        }

        private void UnsubscribePauseRequests()
        {
            _activePauseMenu.CloseButtonClicked -= OnPauseMenuCloseClicked;
            _activePauseMenu.MenuButtonClicked -= OnPauseMenuMenuClicked;
        }

        private void ReleaseWinMenu()
        {
            if (_activeWinMenu == null)
            {
                return;
            }

            UnsubscribeWinRequests();
            _activeWinMenu.Closed -= OnWinMenuClosed;
            _activeWinMenu.BeginClose();
            _uiSpawner.Release(_activeWinMenu);
            _activeWinMenu = null;
        }

        private void ReleaseFailMenu()
        {
            if (_activeFailMenu == null)
            {
                return;
            }

            UnsubscribeFailRequests();
            _activeFailMenu.Closed -= OnFailMenuClosed;
            _activeFailMenu.BeginClose();
            _uiSpawner.Release(_activeFailMenu);
            _activeFailMenu = null;
        }

        private void ReleasePauseMenu()
        {
            if (_activePauseMenu == null)
            {
                return;
            }

            UnsubscribePauseRequests();
            _activePauseMenu.Closed -= OnPauseMenuClosed;
            _activePauseMenu.BeginClose();
            _uiSpawner.Release(_activePauseMenu);
            _activePauseMenu = null;
        }
    }
}
