using UnityEngine;

namespace Skills
{
    [CreateAssetMenu(fileName = "NewAttractConfig", menuName = "Mad Slime/Attract Config")]
    public sealed class AttractConfig : ScriptableObject
    {
        [SerializeField] private float _attractionForce = 6f;
        [SerializeField] private float _approachMultiplier = 3f;
        [SerializeField] private float _approachPower = 2f;

        public float AttractionForce => _attractionForce;
        public float ApproachMultiplier => _approachMultiplier;
        public float ApproachPower => _approachPower;
    }
}
