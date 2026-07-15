using UnityEngine;

namespace Skills
{
    [CreateAssetMenu(fileName = "NewSprintConfig", menuName = "Mad Slime/Sprint Config")]
    public class SprintConfig : SkillConfig
    {
        [SerializeField] private float _duration = 5f;
        [SerializeField] private float _speedMultiplier = 2f;
        [SerializeField] private float _cooldown = 10f;

        public float Duration => _duration;
        public float SpeedMultiplier => _speedMultiplier;
        public float Cooldown => _cooldown;
    }
}
