using Skills;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public sealed class SkillsPanel : MonoBehaviour
    {
        [SerializeField] private SkillManager _skillManager;
        [SerializeField] private SprintSkill _sprintSkill;
        [SerializeField] private AttractSkill _attractSkill;
        [SerializeField] private Button _sprintButton;
        [SerializeField] private Button _attractButton;

        private void OnEnable()
        {
            _skillManager.SkillUnlocked += OnSkillUnlocked;
            _sprintButton.onClick.AddListener(OnSprintButtonClick);
            _attractButton.onClick.AddListener(OnAttractButtonClick);
        }

        private void OnDisable()
        {
            _skillManager.SkillUnlocked -= OnSkillUnlocked;
            _sprintButton.onClick.RemoveListener(OnSprintButtonClick);
            _attractButton.onClick.RemoveListener(OnAttractButtonClick);
        }

        private void Awake()
        {
            _sprintButton.gameObject.SetActive(false);
            _attractButton.gameObject.SetActive(false);
        }

        private void OnSkillUnlocked(SkillId id)
        {
            if (id == SkillId.Sprint)
            {
                _sprintButton.gameObject.SetActive(true);
            }

            if (id == SkillId.Attract)
            {
                _attractButton.gameObject.SetActive(true);
            }
        }

        private void OnSprintButtonClick()
        {
            _sprintSkill.Activate();
        }

        private void OnAttractButtonClick()
        {
            _attractSkill.Activate();
        }
    }
}