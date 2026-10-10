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
        [SerializeField] private RouletteWheel _wheel;
        [SerializeField] private Button _spinButton;
        [SerializeField] private TMP_Text _spinPriceText;
        [SerializeField] private Button _adButton;
        [SerializeField] private TMP_Text _adButtonText;
        [SerializeField] private Sprite _secretIcon;
        [SerializeField] private Sprite _coinIcon;
        [SerializeField] private RouletteWinPopup _winPopupPrefab;
        [SerializeField] private GameObject _screenRoot;
        [SerializeField] private UiScaleAnimator _screenScaleAnimator;
        [SerializeField] private Button _closeButton;

        public event Action SpinRequested;
        public event Action AdRequested;
        public event Action CloseRequested;
        public event Action Closed;
        public event Action Disabled;

        public RouletteWheel Wheel => _wheel;
        public Sprite SecretIcon => _secretIcon;
        public Sprite CoinIcon => _coinIcon;
        public RouletteWinPopup WinPopupPrefab => _winPopupPrefab;

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

            if (_screenScaleAnimator != null)
            {
                _screenScaleAnimator.HideCompleted -= OnHideCompleted;
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

            if (_screenScaleAnimator != null)
            {
                _screenScaleAnimator.PlayShow();
            }
        }

        public void Hide()
        {
            if (_screenScaleAnimator != null)
            {
                _screenScaleAnimator.HideCompleted -= OnHideCompleted;
                _screenScaleAnimator.HideCompleted += OnHideCompleted;
                _screenScaleAnimator.PlayHide();
                return;
            }

            OnHideCompleted();
        }

        public void Display(string spinText, bool canSpin, bool showAd, string adText, bool canWatchAd)
        {
            _spinPriceText.text = spinText;
            _spinButton.interactable = canSpin;

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

        private void OnHideCompleted()
        {
            if (_screenScaleAnimator != null)
            {
                _screenScaleAnimator.HideCompleted -= OnHideCompleted;
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
