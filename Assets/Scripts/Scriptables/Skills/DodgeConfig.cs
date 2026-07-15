using UnityEngine;

namespace Skills
{
    [CreateAssetMenu(fileName = "NewDodgeConfig", menuName = "Mad Slime/Dodge Config")]
    public class DodgeConfig : SkillConfig
    {
        [SerializeField, Range(0f, 1f)] private float _dodgeChance = 0.15f;

        public float DodgeChance => _dodgeChance;
    }
}
