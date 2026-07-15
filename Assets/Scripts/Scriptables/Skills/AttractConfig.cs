using UnityEngine;

namespace Skills
{
    [CreateAssetMenu(fileName = "NewAttractConfig", menuName = "Mad Slime/Attract Config")]
    public class AttractConfig : SkillConfig
    {
        [SerializeField] private float _attractionForce = 6f;
        [SerializeField] private float _activeDuration = 3f;
        [SerializeField] private float _cooldown = 8f;

        public float AttractionForce => _attractionForce;
        public float ActiveDuration => _activeDuration;
        public float Cooldown => _cooldown;
    }
}
