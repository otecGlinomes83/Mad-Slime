using UnityEngine;

namespace Skills
{
    public abstract class BaseSkill : MonoBehaviour
    {
        [SerializeField] private SkillType _type;

        public SkillType Type => _type;

        public abstract bool TryActivate();
    }
}
