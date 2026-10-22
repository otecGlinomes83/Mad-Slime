using System;
using TMPro;
using UnityEngine;

namespace UI
{
    [ExecuteAlways, RequireComponent(typeof(RectTransform)), DisallowMultipleComponent]
    public class CurrencyAmountLayout : MonoBehaviour
    {
        [SerializeField] private RectTransform _icon;
        [SerializeField] private TMP_Text _amount;
        [SerializeField, Min(0f)] private float _spacing = 8f;
        [SerializeField, Min(0f)] private float _padding = 12f;
        [SerializeField, Min(0f)] private float _verticalPadding = 4f;
        [SerializeField, Range(-0.2f, 0.2f)] private float _centerOffset = -0.04f;

        private RectTransform _rect;
        private string _lastText;
        private Vector2 _lastSize;
        private Vector2 _lastIconSize;
        private float _lastMaximumSize;
        private bool _isDirty;

        private void Awake()
        {
            if (_icon == null || _amount == null)
            {
                throw new InvalidOperationException($"{name}: assign the currency icon and amount.");
            }

            _rect = (RectTransform)transform;
        }

        private void OnEnable()
        {
            _isDirty = true;
        }

        private void OnValidate()
        {
            _isDirty = true;
        }

        private void LateUpdate()
        {
            Vector2 size = _rect.rect.size;
            Vector2 iconSize = _icon.rect.size;

            if (_isDirty == false && _lastText == _amount.text && _lastSize == size && _lastIconSize == iconSize
                && _lastMaximumSize == _amount.fontSizeMax)
            {
                return;
            }

            float availableWidth = size.x - iconSize.x - _spacing - _padding * 2f;
            float availableHeight = size.y - _verticalPadding * 2f;

            if (availableWidth <= 0f || availableHeight <= 0f)
            {
                return;
            }

            RectTransform amountRect = _amount.rectTransform;
            amountRect.anchorMin = Vector2.one * 0.5f;
            amountRect.anchorMax = Vector2.one * 0.5f;
            amountRect.pivot = new Vector2(0f, 0.5f);
            amountRect.sizeDelta = new Vector2(availableWidth, availableHeight);
            _amount.horizontalAlignment = HorizontalAlignmentOptions.Left;
            _amount.enableAutoSizing = true;
            _amount.ForceMeshUpdate();

            float textPadding = _amount.margin.x + _amount.margin.z;
            float textWidth = Mathf.Min(availableWidth, _amount.textBounds.size.x + textPadding);
            float groupWidth = iconSize.x + _spacing + textWidth;
            float offsetLimit = Mathf.Max(0f, (size.x - groupWidth) * 0.5f - _padding);
            float offset = Mathf.Clamp(size.x * _centerOffset, -offsetLimit, offsetLimit);
            float left = offset - groupWidth * 0.5f;

            _icon.anchorMin = Vector2.one * 0.5f;
            _icon.anchorMax = Vector2.one * 0.5f;
            _icon.pivot = Vector2.one * 0.5f;
            _icon.sizeDelta = iconSize;
            _icon.anchoredPosition = new Vector2(left + iconSize.x * 0.5f, 0f);
            amountRect.sizeDelta = new Vector2(textWidth, availableHeight);
            amountRect.anchoredPosition = new Vector2(left + iconSize.x + _spacing, 0f);

            _lastText = _amount.text;
            _lastSize = size;
            _lastIconSize = iconSize;
            _lastMaximumSize = _amount.fontSizeMax;
            _isDirty = false;
        }
    }
}
