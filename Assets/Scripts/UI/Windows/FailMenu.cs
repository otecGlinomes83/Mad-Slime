using Game;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public sealed class FailMenu : BaseWindow
    {
        [SerializeField] private TMP_Text _moneyCount;
        [SerializeField] private Button _restartButton;
        [SerializeField] private Button _nextLevelButtonForADS;

        private Action _nextLevelAction;
        private Action _restartAction;

        public void Initialize(int moneyCount, Pauser pauser, Action nextLevelAction, Action restartAction)
        {
            if (_restartButton == null)
            {
                throw new InvalidOperationException(
                    $"{name}: RestartButton is not assigned. Drag a Button into the _restartButton field.");
            }

            if (_nextLevelButtonForADS == null)
            {
                throw new InvalidOperationException(
                    $"{name}: NextLevelButtonForADS is not assigned. Drag a Button into the _nextLevelButtonForADS field.");
            }

            if (_moneyCount == null)
            {
                throw new InvalidOperationException(
                    $"{name}: MoneyCount is not assigned. Drag a TMP_Text into the _moneyCount field.");
            }

            _nextLevelAction = nextLevelAction;
            _restartAction = restartAction;

            _restartButton.onClick.RemoveListener(RequestRestart);
            _nextLevelButtonForADS.onClick.RemoveListener(RequestNextLevel);
            _restartButton.onClick.AddListener(RequestRestart);
            _nextLevelButtonForADS.onClick.AddListener(RequestNextLevel);

            _moneyCount.text = $"{moneyCount}";

            Initialize(pauser);
        }

        protected override void OnDisable()
        {
            _restartButton?.onClick.RemoveListener(RequestRestart);
            _nextLevelButtonForADS?.onClick.RemoveListener(RequestNextLevel);

            base.OnDisable();
        }

        private void RequestNextLevel()
        {
            _nextLevelAction?.Invoke();
            Close();
        }

        private void RequestRestart()
        {
            _restartAction?.Invoke();
            Close();
        }

        private void Close()
        {
            Destroy(gameObject);
        }
    }
}
