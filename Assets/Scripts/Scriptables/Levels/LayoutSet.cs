using System.Collections.Generic;
using UnityEngine;

namespace Scriptables
{
    [CreateAssetMenu(menuName = "Mad Slime/Layout Set", fileName = "NewLayoutSet")]
    public sealed class LayoutSet : ScriptableObject
    {
        [SerializeField] private List<SpawnZone> _zones = new List<SpawnZone>();
        [SerializeField] private bool _allowMirroring = true;

        [Header("Item Placement")]
        [SerializeField] private float _autoSpacingFactor = 2.2f;
        [SerializeField] private float _scatterDistanceFactor = 0.7f;

        public IReadOnlyList<SpawnZone> Zones => _zones;
        public bool AllowMirroring => _allowMirroring;
        public float AutoSpacingFactor => _autoSpacingFactor;
        public float ScatterDistanceFactor => _scatterDistanceFactor;
    }
}
