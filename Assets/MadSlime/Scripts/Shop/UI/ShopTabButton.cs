using System;
using UnityEngine;
using UnityEngine.UI;

namespace Shop
{
    [RequireComponent(typeof(Button))]
    public class ShopTabButton : MonoBehaviour
    {
        [Tooltip("Фон кнопки: его спрайт переключается между обычным и выбранным.")]
        [SerializeField] private Image _background;

        [Tooltip("Спрайт фона невыбранной вкладки.")]
        [SerializeField] private Sprite _backgroundSprite;

        [Tooltip("Спрайт фона выбранной вкладки.")]
        [SerializeField] private Sprite _selectedSprite;

        private bool _isSelected;

        public bool IsSelected => _isSelected;

        private void Awake()
        {
            if (_background == null)
            {
                throw new InvalidOperationException(
                    $"{name}: Background is not assigned. Drag the background Image into the _background field.");
            }

            if (_backgroundSprite == null || _selectedSprite == null)
            {
                throw new InvalidOperationException(
                    $"{name}: a background sprite is not assigned. Drag the background and selected sprites into the fields.");
            }
        }

        public void SetSelected(bool isSelected)
        {
            _isSelected = isSelected;

            if (isSelected == true)
            {
                _background.sprite = _selectedSprite;
                return;
            }

            _background.sprite = _backgroundSprite;
        }
    }
}
