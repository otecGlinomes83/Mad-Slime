using UnityEngine;

namespace Skills
{
    [CreateAssetMenu(fileName = "NewAttractConfig", menuName = "Mad Slime/Attract Config")]
    public sealed class AttractConfig : SkillConfig
    {
        [SerializeField] private float _attractionForce = 6f;

        public float AttractionForce => _attractionForce;
    }
}
