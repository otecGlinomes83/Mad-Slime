using Skills;
using UnityEngine;

namespace Scriptables
{
    [System.Serializable]
    public sealed class SpawnZone
    {
        [SerializeField] private SpawnShape _shape = SpawnShape.Circle;
        [SerializeField] private Vector2 _center = Vector2.zero;
        [SerializeField] private float _radius = 3f;
        [SerializeField] private int _count = 8;
        [SerializeField] private bool _autoSpacing = true;
        [SerializeField] private float _spacing = 0f;
        [SerializeField] private ItemTier _minTier = ItemTier.Small;
        [SerializeField] private ItemTier _maxTier = ItemTier.Small;

        public SpawnShape Shape => _shape;
        public Vector2 Center => _center;
        public float Radius => _radius;
        public int Count => _count;
        public bool AutoSpacing => _autoSpacing;
        public float Spacing => _spacing;
        public ItemTier MinTier => _minTier;
        public ItemTier MaxTier => _maxTier;
    }
}
