using UnityEngine;

namespace Skills
{
    public abstract class SkillConfig : ScriptableObject
    {
        [SerializeField] protected Sprite _icon;
        [SerializeField] protected SkillTier _tier;
        [SerializeField] protected int _requiredLevel = 1;
        [SerializeField] protected string _description;
        [SerializeField] protected float _duration = 3f;
        [SerializeField] protected float _cooldown = 8f;

        public SkillTier Tier => _tier;
        public int RequiredLevel => _requiredLevel;
        public string Description => _description;
        public Sprite Icon => _icon;
        public float Duration => _duration;
        public float Cooldown => _cooldown;
    }
}
