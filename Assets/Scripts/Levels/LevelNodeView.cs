using System;
using TMPro;
using UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Levels
{
    [RequireComponent(typeof(Image))]
    public class LevelNodeView : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] private IntValueView _numberText;
        [SerializeField] private Image _lockIcon;

        [SerializeField] private Sprite _standardBackground;
        [SerializeField] private Sprite _highlightBackground;

        private Image _backgroundImage;
        private int _level;

        public event Action<int> Click;

        public int Level => _level;
        
        
        public void Initialize(int level)
        {
            _backgroundImage = GetComponent<Image>();
            _level = level;
            _numberText.Show(level);
            _backgroundImage.sprite = _standardBackground;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            Click?.Invoke(_level);
        }

        public void Lock()
        {
            _lockIcon.gameObject.SetActive(true);
        }

        public void Unlock()
        {
            _lockIcon.gameObject.SetActive(false);
        }

        public void Select()
        {
            _backgroundImage.sprite = _highlightBackground;
        }

        public void UnSelect()
        {
            _backgroundImage.sprite = _standardBackground;
        }
    }
}