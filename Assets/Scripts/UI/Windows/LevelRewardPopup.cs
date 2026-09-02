using Skills;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public sealed class LevelRewardPopup : MonoBehaviour
    {
        [SerializeField] private IntValueView _levelNumberViewer;
        [SerializeField] private Button _closeButton;
        [SerializeField] private Image _skillIcon;
        [SerializeField] private TMPro.TMP_Text _skillDescription;

        private void OnEnable()
        {
            _closeButton.onClick.AddListener(Close);
        }

        private void OnDisable()
        {
            _closeButton.onClick.RemoveListener(Close);
        }

        public void Initialize(int level, SkillConfig skillConfig)
        {
            _levelNumberViewer.Show(level);
            _skillIcon.sprite = skillConfig.Icon;
            _skillDescription.text = skillConfig.Description;
        }

        private void Close()
        {
            Destroy(gameObject);
        }
    }
}
