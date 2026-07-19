using System;
using UI;
using UnityEngine;
using UnityEngine.UI;

namespace Skins
{
    [RequireComponent(typeof(Image))]
    [RequireComponent(typeof(Button))]
    public class ShopItemView : MonoBehaviour
    {
        [SerializeField] private Sprite _standardBackground;
        [SerializeField] private Sprite _highlightBackground;

        [SerializeField] private Image _contentImage;
        [SerializeField] private Image _lockImage;

        [SerializeField] private IntValueView _priceView;

        [SerializeField] private Image _selectionText;

        private Button _button;
        private Image _backgroundImage;

        public event Action<ShopItemView> Click;

        public SkinItem SkinItem { get; private set; }

        public bool IsLock { get; private set; }

        public int Price => SkinItem.Price;

        public GameObject Model => SkinItem.Model;

        private void OnDisable()
        {
            _button.onClick.RemoveListener(OnClick);
        }
        
        public void Initialize(SkinItem skinItem)
        {
            _backgroundImage = GetComponent<Image>();
            _button = GetComponent<Button>();

            _button.onClick.AddListener(OnClick);

            _backgroundImage.sprite = _standardBackground;

            SkinItem = skinItem;

            _contentImage.sprite = skinItem.Icon;

            _priceView.Show(Price);
        }

        public void OnClick()
        {
            Click?.Invoke(this);
        }

        public void Lock()
        {
            IsLock = true;
            _lockImage.gameObject.SetActive(IsLock);
            _priceView.Show(Price);
        }

        public void Unlock()
        {
            IsLock = false;
            _lockImage.gameObject.SetActive(IsLock);
            _priceView.Hide();
        }

        public void Select()
        {
            _selectionText.gameObject.SetActive(true);
        }

        public void UnSelect()
        {
            _selectionText.gameObject.SetActive(false);
        }

        public void Highlight()
        {
            _backgroundImage.sprite = _highlightBackground;
        }

        public void UnHighlight()
        {
            _backgroundImage.sprite = _standardBackground;
        }
    }
}