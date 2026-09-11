using Game;
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

        private Action _nextLevelAction;
        private Action _doubleRewardAction;

        public void Initialize(int moneyCount, Pauser pauser, Action nextLevelAction, Action doubleRewardAction)
        {
            if (_nextLevelButton == null)
            {
                throw new InvalidOperationException(
                    $"{name}: NextLevelButton is not assigned. Drag a Button into the _nextLevelButton field.");
            }

            if (_moneyCount == null)
            {
                throw new InvalidOperationException(
                    $"{name}: MoneyCount is not assigned. Drag a TMP_Text into the _moneyCount field.");
            }

            _nextLevelAction = nextLevelAction;
            _doubleRewardAction = doubleRewardAction;

            _nextLevelButton.onClick.RemoveListener(RequestNextLevel);
            _nextLevelButton.onClick.AddListener(RequestNextLevel);

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

            Initialize(pauser);
        }

        protected override void OnDisable()
        {
            _nextLevelButton?.onClick.RemoveListener(RequestNextLevel);
            _doubleRewardButton?.onClick.RemoveListener(RequestDoubleReward);

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

        private void Close()
        {
            Destroy(gameObject);
        }
    }
}
