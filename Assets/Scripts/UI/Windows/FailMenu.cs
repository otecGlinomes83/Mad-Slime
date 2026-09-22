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
        [SerializeField] private Button _menuButton;

        private Action _nextLevelAction;
        private Action _restartAction;
        private Action _menuAction;

        public void Initialize(int moneyCount, Action nextLevelAction, Action restartAction, Action menuAction)
        {
            base.Initialize();

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
            _restartAction = restartAction;
            _menuAction = menuAction;

            _restartButton.onClick.RemoveListener(RequestRestart);
            _nextLevelButtonForADS.onClick.RemoveListener(RequestNextLevel);
            _menuButton.onClick.RemoveListener(RequestMenu);
            _restartButton.onClick.AddListener(RequestRestart);
            _nextLevelButtonForADS.onClick.AddListener(RequestNextLevel);
            _menuButton.onClick.AddListener(RequestMenu);

            _moneyCount.text = $"{moneyCount}";
        }

        protected override void OnDisable()
        {
            _restartButton?.onClick.RemoveListener(RequestRestart);
            _nextLevelButtonForADS?.onClick.RemoveListener(RequestNextLevel);
            _menuButton?.onClick.RemoveListener(RequestMenu);

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