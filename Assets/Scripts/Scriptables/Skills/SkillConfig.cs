using UnityEngine;
using UnityEngine.UI;

namespace Skills
{
    public abstract class SkillConfig : ScriptableObject
    {
        [SerializeField] protected Sprite _icon;
        [SerializeField] protected SkillType _type;
        [SerializeField] protected SkillTier _tier;
        [SerializeField] protected int _requiredLevel = 1;
        [SerializeField] protected string _description;
        
        public SkillType Type => _type;
        public SkillTier Tier => _tier;
        public int RequiredLevel => _requiredLevel;
        public string Description => _description;
        public Sprite Icon => _icon;
        
    }
}