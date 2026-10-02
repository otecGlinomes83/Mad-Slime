using System;
using UnityEngine;
using UnityEngine.UI;

namespace Shop
{
    [RequireComponent(typeof(Button))]
    public sealed class ShopTabButton : MonoBehaviour
    {
        [Tooltip("Фон кнопки: его спрайт переключается между вариантами.")]
        [SerializeField] private Image _background;

        [Tooltip("Спрайт фона выбранной вкладки.")]
        [SerializeField] private Sprite _selectedSprite;

        [Tooltip("Спрайт фона невыбранной вкладки.")]
        [SerializeField] private Sprite _unselectedSprite;

        public bool IsSelected { get; private set; }

        private void Awake()
        {
            if (_background == null)
            {
                throw new InvalidOperationException(
                    $"{name}: Background is not assigned. Drag the background Image into the _background field.");
            }

            if (_selectedSprite == null || _unselectedSprite == null)
            {
                throw new InvalidOperationException(
                    $"{name}: a background sprite is not assigned. Drag the selected and unselected sprites into the fields.");
            }
        }

        public void Select()
        {
            IsSelected = true;
            _background.sprite = _selectedSprite;

            Transform group = transform;

            if (transform.parent != null)
            {
                group = transform.parent;
            }

            foreach (Transform child in group)
            {
                if (child == transform)
                {
                    continue;
                }

                if (child.TryGetComponent(out ShopTabButton other) == true)
                {
                    other.Deselect();
                }
            }
        }

        public void Deselect()
        {
            IsSelected = false;
            _background.sprite = _unselectedSprite;
        }
    }
}
