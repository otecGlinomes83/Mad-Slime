using System;
using TMPro;
using UI;
using UI.Animations;
using UnityEngine;
using UnityEngine.UI;

namespace Roulette
{
    public class RouletteView : MonoBehaviour, IShowable
    {
        private const int StackCoinThreshold = 150;
        private const int PileCoinThreshold = 300;
        private const int BagCoinThreshold = 1000;
        private const int SafeCoinThreshold = 3000;

        [SerializeField] private RouletteWheel _wheel;
        [SerializeField] private Button _spinButton;
        [SerializeField] private TMP_Text _spinPriceText;
        [SerializeField] private Button _adButton;
        [SerializeField] private TMP_Text _adButtonText;
        [SerializeField] private Sprite _secretIcon;
        [SerializeField] private Sprite _fewCoinIcon;
        [SerializeField] private Sprite _stackCoinIcon;
        [SerializeField] private Sprite _pileCoinIcon;
        [SerializeField] private Sprite _bagCoinIcon;
        [SerializeField] private Sprite _safeCoinIcon;
        [SerializeField] private RouletteWinPopup _winPopupPrefab;
        [SerializeField] private GameObject _screenRoot;
        [SerializeField] private ReactiveUIView _screenView;
        [SerializeField] private Button _closeButton;
        [SerializeField] private TMP_Text _title;
        [SerializeField] private RoulettePopupLayout _layout;

        public event Action SpinRequested;
        public event Action AdRequested;
        public event Action CloseRequested;
        public event Action Closed;
        public event Action Disabled;

        public RouletteWheel Wheel => _wheel;
        public Sprite SecretIcon => _secretIcon;
        public RouletteWinPopup WinPopupPrefab => _winPopupPrefab;

        public Sprite GetCoinIcon(int amount)
        {
            if (amount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(amount));
            }

            Sprite icon;

            switch (amount)
            {
                case int safeAmount when safeAmount >= SafeCoinThreshold:
                    icon = _safeCoinIcon;
                    break;

                case int bagAmount when bagAmount >= BagCoinThreshold:
                    icon = _bagCoinIcon;
                    break;

                case int pileAmount when pileAmount >= PileCoinThreshold:
                    icon = _pileCoinIcon;
                    break;

                case int stackAmount when stackAmount >= StackCoinThreshold:
                    icon = _stackCoinIcon;
                    break;

                default:
                    icon = _fewCoinIcon;
                    break;
            }

            if (icon == null)
            {
                throw new InvalidOperationException($"{name}: assign the coin reward icon for amount {amount}.");
            }

            return icon;
        }

        private void OnEnable()
        {
            _spinButton.onClick.AddListener(OnSpinClicked);

            if (_adButton != null)
            {
                _adButton.onClick.AddListener(OnAdClicked);
            }

            if (_closeButton != null)
            {
                _closeButton.onClick.AddListener(OnCloseClicked);
            }
        }

        private void OnDisable()
        {
            _spinButton.onClick.RemoveListener(OnSpinClicked);

            if (_adButton != null)
            {
                _adButton.onClick.RemoveListener(OnAdClicked);
            }

            if (_closeButton != null)
            {
                _closeButton.onClick.RemoveListener(OnCloseClicked);
            }

            if (_screenView != null)
            {
                _screenView.DisableCompleted -= OnHideCompleted;
            }

            Disabled?.Invoke();
        }

        public void Show()
        {
            gameObject.SetActive(true);

            if (_screenRoot != null)
            {
                _screenRoot.SetActive(true);
            }

            if (_screenView != null)
            {
                _screenView.DisableCompleted -= OnHideCompleted;
                _screenView.Enable();
            }
        }

        public void Hide()
        {
            if (_screenView != null)
            {
                _screenView.DisableCompleted -= OnHideCompleted;
                _screenView.DisableCompleted += OnHideCompleted;
                _screenView.Disable();
                return;
            }

            OnHideCompleted(null);
        }

        public void Display(string title, bool showSpin, string spinText, bool canSpin, bool showAd, string adText,
            bool canWatchAd, bool showClose, bool canClose)
        {
            _title.text = title;
            if (_layout != null)
            {
                _layout.SetControlsVisible(showSpin, showAd);
            }
            _spinButton.gameObject.SetActive(showSpin);
            _spinPriceText.text = spinText;
            _spinButton.interactable = canSpin;

            if (_closeButton != null)
            {
                _closeButton.gameObject.SetActive(showClose);
                _closeButton.interactable = canClose;
            }

            if (_adButton != null)
            {
                _adButton.gameObject.SetActive(showAd);
                _adButton.interactable = canWatchAd;
            }

            if (_adButtonText != null)
            {
                _adButtonText.text = adText;
            }
        }

        private void OnHideCompleted(ReactiveUIView view)
        {
            if (_screenView != null)
            {
                _screenView.DisableCompleted -= OnHideCompleted;
            }

            Closed?.Invoke();

            if (_screenRoot != null)
            {
                _screenRoot.SetActive(false);
            }
            else
            {
                gameObject.SetActive(false);
            }
        }

        private void OnSpinClicked()
        {
            SpinRequested?.Invoke();
        }

        private void OnAdClicked()
        {
            AdRequested?.Invoke();
        }

        private void OnCloseClicked()
        {
            CloseRequested?.Invoke();
        }
    }
}
