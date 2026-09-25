using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public sealed class WinMenu : BaseWindow
    {
        [SerializeField] private TMP_Text _moneyCount;
        [SerializeField] private Button _nextLevelButton;
        [SerializeField] private Button _doubleRewardButton;
        [SerializeField] private Button _menuButton;

        private Action _nextLevelAction;
        private Action _doubleRewardAction;
        private Action _menuAction;

        public void Initialize(int moneyCount, Action nextLevelAction, Action doubleRewardAction, Action menuAction)
        {
            base.Initialize();

            if (_nextLevelButton == null)
            {
                throw new InvalidOperationException(
                    $"{name}: NextLevelButton is not assigned. Drag a Button into the _nextLevelButton field.");
            }

            if (_menuButton == null)
            {
                throw new InvalidOperationException(
                    $"{name}: MenuButton is not assigned. Drag a Button into the _menuButton field.");
            }

            if (_moneyCount == null)
            {
                throw new InvalidOperationException(
                    $"{name}: MoneyCount is not assigned. Drag a TMP_Text into the _moneyCount field.");
            }

            _nextLevelAction = nextLevelAction;
            _doubleRewardAction = doubleRewardAction;
            _menuAction = menuAction;

            _nextLevelButton.onClick.RemoveListener(RequestNextLevel);
            _nextLevelButton.onClick.AddListener(RequestNextLevel);
            _menuButton.onClick.RemoveListener(RequestMenu);
            _menuButton.onClick.AddListener(RequestMenu);

            if (_doubleRewardButton != null)
            {
                _doubleRewardButton.onClick.RemoveListener(RequestDoubleReward);

                if (doubleRewardAction != null)
                {
                    _doubleRewardButton.onClick.AddListener(RequestDoubleReward);
                    _doubleRewardButton.gameObject.SetActive(true);
                }
                else
                {
                    _doubleRewardButton.gameObject.SetActive(false);
                }
            }

            _moneyCount.text = $"{moneyCount}";
        }

        protected override void OnDisable()
        {
            _nextLevelButton?.onClick.RemoveListener(RequestNextLevel);
            _doubleRewardButton?.onClick.RemoveListener(RequestDoubleReward);
            _menuButton?.onClick.RemoveListener(RequestMenu);

            base.OnDisable();
        }

        private void RequestNextLevel()
        {
            _nextLevelAction?.Invoke();
            Close();
        }

        private void RequestDoubleReward()
        {
            _doubleRewardAction?.Invoke();
            _doubleRewardButton.gameObject.SetActive(false);
        }

        private void RequestMenu()
        {
            _menuAction?.Invoke();
            Close();
        }

        private void Close()
        {
            Destroy(gameObject);
        }
    }
}