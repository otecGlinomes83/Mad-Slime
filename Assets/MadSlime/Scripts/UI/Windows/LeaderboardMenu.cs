using System;
using System.Collections.Generic;
using Game;
using Scriptables;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using YG;
using YG.Utils.LB;

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

            _closeButton.onClick.AddListener(Close);
            _authButton.onClick.AddListener(OnAuthClicked);
            YG2.onGetLeaderboard += OnLeaderboardReceived;
            YG2.onGetSDKData += OnSDKDataReceived;

            RefreshAuthView();
            RequestLeaderboard();
        }

        protected override void OnDisable()
        {
            _closeButton?.onClick.RemoveListener(Close);
            _authButton?.onClick.RemoveListener(OnAuthClicked);
            YG2.onGetLeaderboard -= OnLeaderboardReceived;
            YG2.onGetSDKData -= OnSDKDataReceived;

            base.OnDisable();
        }

        private void RequestLeaderboard()
        {
            _entriesText.text = Localization.Get("leaderboard_loading");
            YG2.GetLeaderboard(_config.LeaderboardName, TopCount, AroundCount, PhotoSize);
        }

        private void OnLeaderboardReceived(LBData data)
        {
            if (data.technoName != _config.LeaderboardName)
            {
                return;
            }

            if (data.entries == InfoYG.NO_DATA || data.players == null || data.players.Length == 0)
            {
                _entriesText.text = Localization.Get("leaderboard_empty");
                return;
            }

            List<string> lines = new List<string>(data.players.Length + 1);
            bool ownRowInTop = false;

            for (int i = 0; i < data.players.Length; i++)
            {
                LBPlayerData player = data.players[i];
                string line = $"{player.rank}. {LBMethods.AnonymousName(player.name)} — {player.score}";

                if (player.uniqueID == YG2.player.id)
                {
                    ownRowInTop = true;
                    line = $"<b>{line}</b>";
                }

                lines.Add(line);
            }

            if (ownRowInTop == false && YG2.player.auth && data.currentPlayer != null && data.currentPlayer.rank > 0)
            {
                lines.Add($"<b>{data.currentPlayer.rank}. {YG2.player.name} — {data.currentPlayer.score}</b>");
            }

            _entriesText.text = string.Join("\n", lines);
        }

        private void OnSDKDataReceived()
        {
            RefreshAuthView();

            if (YG2.player.auth)
            {
                RequestLeaderboard();
            }
        }

        private void OnAuthClicked()
        {
            YG2.OpenAuthDialog();
            RefreshAuthView();
        }

        private void RefreshAuthView()
        {
            _authButton.gameObject.SetActive(YG2.player.auth == false);
        }

        private void Close()
        {
            Destroy(gameObject);
        }
    }
}
