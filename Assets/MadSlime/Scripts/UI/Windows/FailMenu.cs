using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class FailMenu : BaseWindow
    {
        [SerializeField] private TMP_Text _moneyCount;
        [SerializeField] private Button _rescueButton;
        [SerializeField] private Button _restartButton;
        [SerializeField] private Button _menuButton;

        private bool _isNavigationRequested;

        public event Action RescueRequested;

        public event Action RestartRequested;

        public event Action MenuRequested;

        protected override void Awake()
        {
            base.Awake();

            if (_moneyCount == null)
            {
                throw new InvalidOperationException(
                    $"{name}: MoneyCount is not assigned. Drag a TMP_Text into the _moneyCount field.");
            }

            if (_rescueButton == null)
            {
                throw new InvalidOperationException(
                    $"{name}: RescueButton is not assigned. Drag a Button into the _rescueButton field.");
            }

            if (_restartButton == null)
            {
                throw new InvalidOperationException(
                    $"{name}: RestartButton is not assigned. Drag a Button into the _restartButton field.");
            }

            if (_menuButton == null)
            {
                throw new InvalidOperationException(
                    $"{name}: MenuButton is not assigned. Drag a Button into the _menuButton field.");
            }
        }

        public void Initialize(int moneyCount, bool canRescue)
        {
            _isNavigationRequested = false;
            _restartButton.interactable = true;
            _menuButton.interactable = true;
            _moneyCount.text = $"{moneyCount}";

            _rescueButton.interactable = canRescue;
            _rescueButton.gameObject.SetActive(canRescue);
        }

        public void LockRescue()
        {
            _rescueButton.interactable = false;
        }

        private void OnEnable()
        {
            _rescueButton.onClick.AddListener(OnRescueClicked);
            _restartButton.onClick.AddListener(OnRestartClicked);
            _menuButton.onClick.AddListener(OnMenuClicked);
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            _rescueButton.onClick.RemoveListener(OnRescueClicked);
            _restartButton.onClick.RemoveListener(OnRestartClicked);
            _menuButton.onClick.RemoveListener(OnMenuClicked);
        }

        protected override void OnClosing()
        {
            _rescueButton.interactable = false;
            _restartButton.interactable = false;
            _menuButton.interactable = false;
        }

        private void OnRescueClicked()
        {
            if (_isNavigationRequested == true || IsClosing == true)
            {
                return;
            }

            Action rescueRequested = RescueRequested;
            rescueRequested?.Invoke();
        }

        private void OnRestartClicked()
        {
            if (_isNavigationRequested == true || IsClosing == true)
            {
                return;
            }

            _isNavigationRequested = true;
            Action restartRequested = RestartRequested;
            restartRequested?.Invoke();

        }

        private void OnMenuClicked()
        {
            if (_isNavigationRequested == true || IsClosing == true)
            {
                return;
            }

            _isNavigationRequested = true;
            Action menuRequested = MenuRequested;
            menuRequested?.Invoke();

        }
    }
}
