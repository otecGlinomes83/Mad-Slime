using System;
using System.Collections.Generic;
using System.Threading;
using Core;
using Cysharp.Threading.Tasks;
using Game;
using Saves;
using Scriptables;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using VContainer;

namespace UI
{
    public class LeaderboardMenu : BaseWindow
    {
        private const int TopCount = 10;
        private const int SdkTimeoutSeconds = 8;

        private enum State
        {
            Loading,
            AuthRequired,
            Ready,
            Empty,
            Error
        }

        [SerializeField] private Button _closeButton;
        [SerializeField] private Button _authButton;
        [FormerlySerializedAs("_entriesText")]
        [SerializeField] private TMP_Text _statusText;
        [SerializeField] private Button _retryButton;
        [SerializeField] private LeaderboardRowViewFactory _rowFactory;
        [SerializeField] private ScrollRect _scroll;
        [SerializeField] private RectTransform _panel;
        [SerializeField] private YandexConfig _config;

        private ISavesReadiness _savesReadiness;
        private ILeaderboardService _leaderboardService;
        private List<LeaderboardRowView> _rows = new List<LeaderboardRowView>();
        private LeaderboardRowView _ownRow;
        private CancellationTokenSource _loadSource;
        private State _state;
        private Vector2Int _screenSize;
        private float _maximumPanelHeight;

        [Inject]
        public void Construct(ISavesReadiness savesReadiness, ILeaderboardService leaderboardService)
        {
            _savesReadiness = savesReadiness;
            _leaderboardService = leaderboardService;
        }

        public void Initialize()
        {
            if (_closeButton == null || _authButton == null || _retryButton == null
                || _rowFactory == null || _scroll == null || _scroll.content == null || _scroll.viewport == null
                || _panel == null || _config == null
                || string.IsNullOrEmpty(_config.LeaderboardName))
            {
                throw new InvalidOperationException($"{name}: leaderboard window references are incomplete.");
            }

            if (_savesReadiness == null || _leaderboardService == null)
            {
                throw new InvalidOperationException($"{name}: show the leaderboard through UiSpawner with injected dependencies.");
            }

            _closeButton.onClick.AddListener(OnCloseClicked);
            _authButton.onClick.AddListener(OnAuthClicked);
            _retryButton.onClick.AddListener(OnRetryClicked);
            _savesReadiness.Ready += OnSdkDataReceived;
            Localization.LanguageChanged += OnLanguageChanged;
            RememberPanelHeight();
            RequestLeaderboard();
        }

        private void LateUpdate()
        {
            if (_panel == null || _scroll == null)
            {
                return;
            }

            if (_screenSize.x != Screen.width || _screenSize.y != Screen.height)
            {
                RememberPanelHeight();
            }

            if (_state != State.Ready || _rows.Count == 0)
            {
                return;
            }

            float contentHeight = _scroll.content.rect.height;

            if (contentHeight <= 0f)
            {
                return;
            }

            float height = Mathf.Min(_maximumPanelHeight,
                _panel.rect.height + contentHeight - _scroll.viewport.rect.height);

            if (Mathf.Abs(height - _panel.rect.height) > 1f)
            {
                _panel.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
            }
        }

        private void RememberPanelHeight()
        {
            _screenSize = new Vector2Int(Screen.width, Screen.height);
            _maximumPanelHeight = _panel.rect.height;
        }

        protected override void OnClosing()
        {
            base.OnClosing();
            CancelLoading();
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            CancelLoading();
            _closeButton?.onClick.RemoveListener(OnCloseClicked);
            _authButton?.onClick.RemoveListener(OnAuthClicked);
            _retryButton?.onClick.RemoveListener(OnRetryClicked);
            Localization.LanguageChanged -= OnLanguageChanged;

            if (_savesReadiness != null)
            {
                _savesReadiness.Ready -= OnSdkDataReceived;
            }
        }

        private void RequestLeaderboard()
        {
            if (IsClosing == true)
            {
                return;
            }

            CancelLoading();
            _loadSource = CancellationTokenSource.CreateLinkedTokenSource(this.GetCancellationTokenOnDestroy());
            LoadAsync(_loadSource.Token).Forget();
        }

        private async UniTaskVoid LoadAsync(CancellationToken cancellationToken)
        {
            SetState(State.Loading);
            _retryButton.interactable = false;

            try
            {
                await UniTask.WaitUntil(IsSdkReady, cancellationToken: cancellationToken)
                    .Timeout(TimeSpan.FromSeconds(SdkTimeoutSeconds), DelayType.Realtime);
                cancellationToken.ThrowIfCancellationRequested();

                if (_leaderboardService.IsAuthorized == false)
                {
                    ClearRows();
                    SetState(State.AuthRequired);
                    return;
                }

                LeaderboardData snapshot = await _leaderboardService.RequestEntriesAsync(
                    _config.LeaderboardName, TopCount, cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();
                Render(snapshot);
                await ScrollToPlayerAsync(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch (Exception)
            {
                if (cancellationToken.IsCancellationRequested == false)
                {
                    SetState(State.Error);
                }
            }
        }

        private bool IsSdkReady()
        {
            return _savesReadiness.IsReady;
        }

        private void Render(LeaderboardData snapshot)
        {
            ClearRows();

            for (int i = 0; i < snapshot.Players.Length; i++)
            {
                LeaderboardEntryData entry = snapshot.Players[i];
                bool isOwn = _leaderboardService.IsAuthorized == true && entry.Id == _leaderboardService.PlayerId;
                AddRow(entry, isOwn);
            }

            if (_ownRow == null && snapshot.CurrentPlayer.HasValue == true)
            {
                AddRow(snapshot.CurrentPlayer.Value, true);
            }

            if (_rows.Count == 0)
            {
                SetState(State.Empty);
            }
            else
            {
                SetState(State.Ready);
            }

            if (snapshot.HasPlayerError == true)
            {
                SetState(State.Error);
            }
        }

        private void AddRow(LeaderboardEntryData entry, bool isOwn)
        {
            LeaderboardRowView row = _rowFactory.Create(_scroll.content, entry, isOwn);
            _rows.Add(row);

            if (isOwn == true)
            {
                _ownRow = row;
            }
        }

        private async UniTask ScrollToPlayerAsync(CancellationToken cancellationToken)
        {
            await UniTask.Yield(PlayerLoopTiming.LastPostLateUpdate, cancellationToken);
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(_scroll.content);
            _scroll.StopMovement();
            _scroll.verticalNormalizedPosition = 1f;

            if (_ownRow == null)
            {
                return;
            }

            RectTransform content = _scroll.content;
            RectTransform viewport = _scroll.viewport;
            Vector3 viewportTop = content.InverseTransformPoint(
                viewport.TransformPoint(new Vector3(0f, viewport.rect.yMax, 0f)));
            Vector3 viewportBottom = content.InverseTransformPoint(
                viewport.TransformPoint(new Vector3(0f, viewport.rect.yMin, 0f)));
            float viewportHeight = viewportTop.y - viewportBottom.y;
            float scrollableHeight = content.rect.height - viewportHeight;

            if (scrollableHeight <= 0f)
            {
                return;
            }

            RectTransform row = _ownRow.RectTransform;
            Vector3 rowBottom = content.InverseTransformPoint(
                row.TransformPoint(new Vector3(row.rect.center.x, row.rect.yMin, 0f)));
            float targetOffset = content.rect.yMax - rowBottom.y - viewportHeight;
            float clampedOffset = Mathf.Clamp(targetOffset, 0f, scrollableHeight);
            _scroll.verticalNormalizedPosition = 1f - clampedOffset / scrollableHeight;
        }

        private void SetState(State state)
        {
            _state = state;
            _authButton.gameObject.SetActive(state == State.AuthRequired);
            _scroll.gameObject.SetActive(state != State.AuthRequired && _rows.Count > 0);
            _retryButton.gameObject.SetActive(state == State.Empty || state == State.Error);
            _retryButton.interactable = state != State.Loading;
            OnLanguageChanged();
        }

        private void OnLanguageChanged()
        {
            if (_statusText == null)
            {
                return;
            }

            _statusText.gameObject.SetActive(_state != State.Ready);

            switch (_state)
            {
                case State.AuthRequired:
                    _statusText.text = Localization.Get("leaderboard_login_hint");
                    break;
                case State.Loading:
                    _statusText.text = Localization.Get("leaderboard_loading");
                    break;
                case State.Empty:
                    _statusText.text = Localization.Get("leaderboard_empty");
                    break;
                case State.Error:
                    _statusText.text = Localization.Get("leaderboard_error");
                    break;
                case State.Ready:
                    _statusText.text = string.Empty;
                    break;
            }
        }

        private void ClearRows()
        {
            for (int i = 0; i < _rows.Count; i++)
            {
                _rows[i].gameObject.SetActive(false);
                Destroy(_rows[i].gameObject);
            }

            _rows.Clear();
            _ownRow = null;
        }

        private void CancelLoading()
        {
            if (_loadSource == null)
            {
                return;
            }

            _loadSource.Cancel();
            _loadSource.Dispose();
            _loadSource = null;
        }

        private void OnRetryClicked()
        {
            if (_state == State.Loading)
            {
                return;
            }

            RequestLeaderboard();
        }

        private void OnSdkDataReceived()
        {
            RequestLeaderboard();
        }

        private void OnAuthClicked()
        {
            if (_state != State.AuthRequired || _leaderboardService.IsAuthorized == true)
            {
                return;
            }

            _leaderboardService.OpenAuthDialog();
        }

        private void OnCloseClicked()
        {
            RequestClose();
        }
    }
}
