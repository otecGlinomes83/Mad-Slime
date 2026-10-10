using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Shop
{
    [RequireComponent(typeof(Image), typeof(Button))]
    public class UpgradeItemView : MonoBehaviour
    {
        [SerializeField] private Image _icon;
        [SerializeField] private TMP_Text _stepText;
        [SerializeField] private TMP_Text _titleText;
        [SerializeField] private TMP_Text _effectText;
        [SerializeField] private TMP_Text _priceText;

        private Button _button;

        public event Action<UpgradeItemView> Click;

        private void Awake()
        {
            TryGetComponent(out _button);
        }

        private void OnEnable()
        {
            _button.onClick.AddListener(OnClick);
        }

        private void OnDisable()
        {
            _button.onClick.RemoveListener(OnClick);
        }

        public void Initialize(Sprite icon, string title, string effect, bool showStep)
        {
            _icon.sprite = icon;
            _icon.enabled = icon != null;
            _titleText.text = title;
            _effectText.text = effect;
            _stepText.gameObject.SetActive(showStep);
        }

        public void SetProgress(string step, string effect)
        {
            _stepText.text = step;
            _effectText.text = effect;
        }

        public void SetPurchaseState(string price, bool isAvailable)
        {
            _priceText.text = price;
            _button.interactable = isAvailable;
        }

        private void OnClick()
        {
            Click?.Invoke(this);
        }
    }
}
