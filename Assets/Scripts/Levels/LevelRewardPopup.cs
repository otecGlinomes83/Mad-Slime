using System;
using Skills;
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
        [SerializeField] private Image _skillIcon;
        [SerializeField] private TMP_Text _skillDescription;
        
        public event Action CloseClicked;

        private void OnEnable()
        {
            _closeButton.onClick.AddListener(Close);
        }

        private void OnDisable()
        {
            _closeButton.onClick.RemoveListener(Close);
        }

        public void Initialize(int level,SkillConfig skillConfig)
        {
            _levelNumberViewer.Show(level);
            _skillIcon.sprite = skillConfig.Icon;
            _skillDescription.text = skillConfig.Description;
        }

        public void SimpleInitialize(int level)
        {
            _levelNumberViewer.Show(level);
            _skillDescription.text = $"На этом уровне нет улучшений для способностей.";
        }
        
        private void Close()
        {
            Destroy(gameObject);
        }
    }
}