using System.Collections.Generic;
using UnityEngine;

namespace Scriptables
{
    [CreateAssetMenu(menuName = "Mad Slime/Layout Set", fileName = "NewLayoutSet")]
    public sealed class LayoutSet : ScriptableObject
    {
        [SerializeField] private List<SpawnZone> _zones = new List<SpawnZone>();
        [SerializeField] private bool _allowMirroring = true;

        public IReadOnlyList<SpawnZone> Zones => _zones;
        public bool AllowMirroring => _allowMirroring;
    }
}
