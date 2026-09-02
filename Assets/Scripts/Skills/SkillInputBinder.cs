using PlayerInput;
using UnityEngine;

namespace Skills
{
    public sealed class SkillInputBinder : MonoBehaviour
    {
        [SerializeField] private PlayerInputReader _inputReader;
        [SerializeField] private SkillHandler _skillHandler;
        [SerializeField] private SkillUnlocker _skillUnlocker;
        [SerializeField] private SkillConfig _attractSkillConfig;

        private void OnEnable()
        {
            _inputReader.AttractPerformed += OnAttractPerformed;
        }

        private void OnDisable()
        {
            _inputReader.AttractPerformed -= OnAttractPerformed;
        }

        private void OnAttractPerformed()
        {
            if (_skillUnlocker.IsUnlocked(_attractSkillConfig) == false)
            {
                return;
            }

            _skillHandler.TryActivate(_attractSkillConfig);
        }
    }
}
