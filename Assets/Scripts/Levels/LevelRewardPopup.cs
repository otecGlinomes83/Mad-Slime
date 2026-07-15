using System;
using TMPro;
using UI;
using UnityEngine;
using UnityEngine.UI;

namespace Levels.Levels
{
    public class LevelRewardPopup : MonoBehaviour
    {
        [SerializeField] private IntValueView _levelNumberViewer;
        [SerializeField] private Button _closeButton;

        public event Action CloseClicked;

        private void OnEnable()
        {
            _closeButton.onClick.AddListener(Close);
        }

        private void OnDisable()
        {
            _closeButton.onClick.RemoveListener(Close);
        }

        public void Show(int level)
        {
            _levelNumberViewer.Show(level);
        }

        private void Close()
        {
            Destroy(gameObject);
        }
    }
}