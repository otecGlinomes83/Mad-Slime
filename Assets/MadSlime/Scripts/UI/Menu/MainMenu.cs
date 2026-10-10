using System;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class MainMenu : MonoBehaviour
    {
        [SerializeField] private Button _playButton;
        [SerializeField] private Button _shopButton;
        [SerializeField] private Button _leaderboardButton;
        [SerializeField] private Button _settingsButton;
        [SerializeField] private Button _dailyButton;

        public event Action PlayClicked;

        public event Action ShopClicked;

        public event Action LeaderboardClicked;

        public event Action SettingsClicked;

        public event Action DailyClicked;

        private void Awake()
        {
            if (_playButton == null)
            {
                throw new InvalidOperationException(
                    $"{name}: PlayButton is not assigned. Drag a Button into the _playButton field.");
            }

            if (_shopButton == null)
            {
                throw new InvalidOperationException(
                    $"{name}: ShopButton is not assigned. Drag a Button into the _shopButton field.");
            }

            if (_leaderboardButton == null)
            {
                throw new InvalidOperationException(
                    $"{name}: LeaderboardButton is not assigned. Drag a Button into the _leaderboardButton field.");
            }

            if (_settingsButton == null)
            {
                throw new InvalidOperationException(
                    $"{name}: SettingsButton is not assigned. Drag a Button into the _settingsButton field.");
            }

            if (_dailyButton == null)
            {
                throw new InvalidOperationException(
                    $"{name}: DailyButton is not assigned. Drag a Button into the _dailyButton field.");
            }
        }

        private void OnEnable()
        {
            _playButton.onClick.AddListener(OnPlayClicked);
            _shopButton.onClick.AddListener(OnShopClicked);
            _leaderboardButton.onClick.AddListener(OnLeaderboardClicked);
            _settingsButton.onClick.AddListener(OnSettingsClicked);
            _dailyButton.onClick.AddListener(OnDailyClicked);
        }

        private void OnDisable()
        {
            _playButton.onClick.RemoveListener(OnPlayClicked);
            _shopButton.onClick.RemoveListener(OnShopClicked);
            _leaderboardButton.onClick.RemoveListener(OnLeaderboardClicked);
            _settingsButton.onClick.RemoveListener(OnSettingsClicked);
            _dailyButton.onClick.RemoveListener(OnDailyClicked);
        }

        private void OnPlayClicked()
        {
            PlayClicked?.Invoke();
        }

        private void OnShopClicked()
        {
            ShopClicked?.Invoke();
        }

        private void OnLeaderboardClicked()
        {
            LeaderboardClicked?.Invoke();
        }

        private void OnSettingsClicked()
        {
            SettingsClicked?.Invoke();
        }

        private void OnDailyClicked()
        {
            DailyClicked?.Invoke();
        }
    }
}
