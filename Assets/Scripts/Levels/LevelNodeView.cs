using System;
using TMPro;
using UI;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Levels
{
    [RequireComponent(typeof(Image))]
    [RequireComponent(typeof(Button))]
    public class LevelNodeView : MonoBehaviour
    {
        [SerializeField] private IntValueView _numberText;
        [SerializeField] private Image _lockIcon;

        [SerializeField] private Sprite _standardBackground;
        [SerializeField] private Sprite _highlightBackground;
        [SerializeField] private Sprite _completedBackground;

        private Image _backgroundImage;
        private int _level;

        private Button _button;
        
        public event Action<int> Click;

        public int Level => _level;

        private void OnDisable()
        {
            _button.onClick.RemoveListener(OnClick);
        }

        public void Initialize(int level)
        {
            _backgroundImage = GetComponent<Image>();
            _button = GetComponent<Button>();
            
            _button.onClick.AddListener(OnClick);
            
            _level = level;
            _numberText.Show(level);
            _backgroundImage.sprite = _standardBackground;
        }

        public void OnClick()
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

        public void Complete()
        {
            _backgroundImage.sprite = _completedBackground;
        }
    }
}