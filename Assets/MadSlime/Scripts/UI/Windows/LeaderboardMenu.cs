using Core;
using Game;
using Scriptables;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace UI
{
    public sealed class LeaderboardMenu : BaseWindow
    {
        private const int TopCount = 10;
        private const int AroundCount = 1;
        private const string PhotoSize = "nonePhoto";

        [SerializeField] private Button _closeButton;
        [SerializeField] private Button _authButton;
        [SerializeField] private TMP_Text _entriesText;
        [SerializeField] private YandexConfig _config;

        private PlayerProgress _progress;
        private ILeaderboardService _leaderboardService;

        [Inject]
        public void Construct(PlayerProgress progress, ILeaderboardService leaderboardService)
        {
            _progress = progress;
            _leaderboardService = leaderboardService;
        }

        public override void Initialize()
        {
            base.Initialize();

            if (_closeButton == null)
            {
                throw new InvalidOperationException(
                    $"{name}: CloseButton is not assigned. Drag a Button into the _closeButton field.");
            }

            if (_authButton == null)
            {
                throw new InvalidOperationException(
                    $"{name}: AuthButton is not assigned. Drag a Button into the _authButton field.");
            }

            if (_entriesText == null)
            {
                throw new InvalidOperationException(
                    $"{name}: EntriesText is not assigned. Drag a TMP_Text into the _entriesText field.");
            }

            if (_config == null)
            {
                throw new InvalidOperationException(
                    $"{name}: YandexConfig is not assigned. Drag the YandexConfig asset into the _config field.");
            }

            if (_progress == null || _leaderboardService == null)
            {
                throw new InvalidOperationException(
                    $"{name}: dependencies were not injected. The window prefab must be instantiated through the DI container (IObjectResolver.Instantiate).");
            }

            _closeButton.onClick.AddListener(Close);
            _authButton.onClick.AddListener(OnAuthClicked);
            _leaderboardService.EntriesReceived += OnLeaderboardReceived;
            _progress.Ready += OnSdkDataReceived;

            RefreshAuthView();
            RequestLeaderboard();
        }

        protected override void OnDisable()
        {
            _closeButton?.onClick.RemoveListener(Close);
            _authButton?.onClick.RemoveListener(OnAuthClicked);
            _leaderboardService.EntriesReceived -= OnLeaderboardReceived;
            _progress.Ready -= OnSdkDataReceived;

            base.OnDisable();
        }

        private void RequestLeaderboard()
        {
            _entriesText.text = Localization.Get("leaderboard_loading");
            _leaderboardService.RequestEntries(_config.LeaderboardName, TopCount, AroundCount, PhotoSize);
        }

        private void OnLeaderboardReceived(LeaderboardSnapshot snapshot)
        {
            if (snapshot.TechnoName != _config.LeaderboardName)
            {
                return;
            }

            if (snapshot.HasEntries == false || snapshot.Players.Length == 0)
            {
                _entriesText.text = Localization.Get("leaderboard_empty");
                return;
            }

            List<string> lines = new List<string>(snapshot.Players.Length + 1);
            bool ownRowInTop = false;

            for (int entryIndex = 0; entryIndex < snapshot.Players.Length; entryIndex++)
            {
                LeaderboardEntryData entry = snapshot.Players[entryIndex];
                string line = $"{entry.Rank}. {entry.Name} — {entry.Score}";

                if (entry.Id == _leaderboardService.PlayerId)
                {
                    ownRowInTop = true;
                    line = $"<b>{line}</b>";
                }

                lines.Add(line);
            }

            if (ownRowInTop == false && _leaderboardService.IsAuthorized && snapshot.HasCurrentPlayer && snapshot.CurrentPlayer.Rank > 0)
            {
                lines.Add($"<b>{snapshot.CurrentPlayer.Rank}. {_leaderboardService.PlayerName} — {snapshot.CurrentPlayer.Score}</b>");
            }

            _entriesText.text = string.Join("\n", lines);
        }

        private void OnSdkDataReceived()
        {
            RefreshAuthView();

            if (_leaderboardService.IsAuthorized)
            {
                RequestLeaderboard();
            }
        }

        private void OnAuthClicked()
        {
            _leaderboardService.OpenAuthDialog();
            RefreshAuthView();
        }

        private void RefreshAuthView()
        {
            _authButton.gameObject.SetActive(_leaderboardService.IsAuthorized == false);
        }

        private void Close()
        {
            Destroy(gameObject);
        }
    }
}
