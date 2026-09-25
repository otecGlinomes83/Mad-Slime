using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public sealed class FailMenu : BaseWindow
    {
        [SerializeField] private TMP_Text _moneyCount;
        [SerializeField] private Button _rescueButton;
        [SerializeField] private Button _restartButton;
        [SerializeField] private Button _menuButton;

        private Action _rescueAction;
        private Action _restartAction;
        private Action _menuAction;

        public void Initialize(int moneyCount, Action rescueAction, bool canRescue, Action restartAction, Action menuAction)
        {
            base.Initialize();

            if (_restartButton == null)
            {
                throw new InvalidOperationException(
                    $"{name}: RestartButton is not assigned. Drag a Button into the _restartButton field.");
            }

            if (_rescueButton == null)
            {
                throw new InvalidOperationException(
                    $"{name}: RescueButton is not assigned. Drag a Button into the _rescueButton field.");
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

            _rescueAction = rescueAction;
            _restartAction = restartAction;
            _menuAction = menuAction;

            _rescueButton.onClick.RemoveListener(RequestRescue);
            _restartButton.onClick.RemoveListener(RequestRestart);
            _menuButton.onClick.RemoveListener(RequestMenu);
            _rescueButton.onClick.AddListener(RequestRescue);
            _restartButton.onClick.AddListener(RequestRestart);
            _menuButton.onClick.AddListener(RequestMenu);

            _rescueButton.gameObject.SetActive(canRescue == true && rescueAction != null);

            _moneyCount.text = $"{moneyCount}";
        }

        protected override void OnDisable()
        {
            _rescueButton?.onClick.RemoveListener(RequestRescue);
            _restartButton?.onClick.RemoveListener(RequestRestart);
            _menuButton?.onClick.RemoveListener(RequestMenu);

            base.OnDisable();
        }

        private void RequestRescue()
        {
            _rescueButton.interactable = false;
            _rescueAction?.Invoke();
        }

        public void OnRescueRejected()
        {
            _rescueButton.interactable = true;
        }

        public void Dismiss()
        {
            Destroy(gameObject);
        }

        private void RequestRestart()
        {
            _restartAction?.Invoke();
            Destroy(gameObject);
        }

        private void RequestMenu()
        {
            _menuAction?.Invoke();
            Destroy(gameObject);
        }
    }
}
