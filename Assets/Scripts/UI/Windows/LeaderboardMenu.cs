using System;
using System.Collections.Generic;
using Game;
using Scriptables;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public sealed class LeaderboardMenu : BaseWindow
    {
        private const int TopCount = 10;

        [SerializeField] private Button _closeButton;
        [SerializeField] private TMP_Text _entriesText;
        [SerializeField] private YandexConfig _config;

        private YandexAdsBridge _bridge;

        public override void Initialize(Pauser pauser)
        {
            if (_closeButton == null)
            {
                throw new InvalidOperationException(
                    $"{name}: CloseButton is not assigned. Drag a Button into the _closeButton field.");
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

            _bridge = YandexAdsBridge.Create();
            _bridge.EntriesReceived += OnEntriesReceived;
            _bridge.EntriesError += OnEntriesError;

            _closeButton.onClick.AddListener(Close);

            base.Initialize(pauser);

            _entriesText.text = Localization.Get("leaderboard_loading");
            _bridge.GetLeaderboardEntries(_config.LeaderboardName, TopCount);
        }

        protected override void OnDisable()
        {
            _closeButton?.onClick.RemoveListener(Close);

            if (_bridge != null)
            {
                _bridge.EntriesReceived -= OnEntriesReceived;
                _bridge.EntriesError -= OnEntriesError;
                Destroy(_bridge.gameObject);
                _bridge = null;
            }

            base.OnDisable();
        }

        private void OnEntriesReceived(string jsonPayload)
        {
            YandexAdsBridge.LeaderboardPayload payload = JsonUtility.FromJson<YandexAdsBridge.LeaderboardPayload>(jsonPayload);

            if (payload == null || payload.entries == null || payload.entries.Length == 0)
            {
                _entriesText.text = Localization.Get("leaderboard_empty");
                return;
            }

            List<string> lines = new List<string>(payload.entries.Length);

            for (int i = 0; i < payload.entries.Length; i++)
            {
                YandexAdsBridge.LeaderboardEntry entry = payload.entries[i];
                string line = $"{entry.rank}. {ResolvePlayerName(entry)} — {entry.score}";

                if (payload.userRank == entry.rank)
                {
                    line = $"<b>{line}</b>";
                }

                lines.Add(line);
            }

            _entriesText.text = string.Join("\n", lines);
        }

        private static string ResolvePlayerName(YandexAdsBridge.LeaderboardEntry entry)
        {
            if (string.IsNullOrEmpty(entry.name) == false)
            {
                return entry.name;
            }

            if (string.IsNullOrEmpty(entry.extra) == false)
            {
                return entry.extra;
            }

            return "—";
        }

        private void OnEntriesError(string leaderboardName)
        {
            _entriesText.text = Localization.Get("leaderboard_error");
        }

        private void Close()
        {
            Destroy(gameObject);
        }
    }
}
