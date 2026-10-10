using System;
using UnityEngine;
using UnityEngine.UI;

namespace Shop
{
    [RequireComponent(typeof(Image), typeof(Button))]
    public class ShopItemView : MonoBehaviour
    {
        [SerializeField] private Image _backgroundImage;
        [SerializeField] private Image _contentImage;
        [SerializeField] private Image _lockImage;
        [SerializeField] private GameObject _rouletteBadge;
        [SerializeField] private Image _selectedMark;

        private Button _button;
        private string _skinId;

        public event Action<ShopItemView> Click;
        public string SkinId => _skinId;

        private void Awake()
        {
            TryGetComponent(out _button);

            if (_backgroundImage == null)
            {
                TryGetComponent(out _backgroundImage);
            }
        }

        private void OnEnable()
        {
            _button.onClick.AddListener(OnClick);
        }

        private void OnDisable()
        {
            _button.onClick.RemoveListener(OnClick);
        }

        public void Initialize(string skinId, Sprite icon, Color rarityColor, bool isExclusive, bool isUnlocked, bool isSelected)
        {
            _skinId = skinId;
            _contentImage.sprite = icon;
            _backgroundImage.color = rarityColor;
            _rouletteBadge.SetActive(isExclusive);
            SetUnlocked(isUnlocked);
            SetSelected(isSelected);
        }

        public void SetUnlocked(bool isUnlocked)
        {
            _lockImage.gameObject.SetActive(isUnlocked == false);
        }

        public void SetSelected(bool isSelected)
        {
            _selectedMark.gameObject.SetActive(isSelected);
        }

        private void OnClick()
        {
            Click?.Invoke(this);
        }
    }
}
