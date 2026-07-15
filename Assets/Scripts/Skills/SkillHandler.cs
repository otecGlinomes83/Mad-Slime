using System.Collections.Generic;
using UnityEngine;

namespace Skills
{
    public sealed class SkillHandler : MonoBehaviour
    {
        [SerializeField] private List<BaseSkill> _skills;

        public bool IsUnlocked(SkillType type)
        {
            for (int i = 0; i < _skills.Count; i++)
            {
                if (_skills[i].Type == type)
                {
                    return true;
                }
            }

            return false;
        }

        public bool TryActivate(SkillType type)
        {
            for (int i = 0; i < _skills.Count; i++)
            {
                BaseSkill skill = _skills[i];
                if (skill.Type != type)
                {
                    continue;
                }

                return skill.TryActivate();
            }

            return false;
        }
    }
}
