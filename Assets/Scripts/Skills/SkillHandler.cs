using System.Collections.Generic;
using UnityEngine;

namespace Skills
{
    public sealed class SkillHandler : MonoBehaviour
    {
        [SerializeField] private List<BaseSkill> _skills;

        public bool TryActivate(SkillConfig config)
        {
            for (int i = 0; i < _skills.Count; i++)
            {
                if (_skills[i].Config == config)
                {
                    return _skills[i].TryActivate();
                }
            }

            return false;
        }
    }
}
