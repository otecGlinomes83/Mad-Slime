using System.Collections.Generic;
using UnityEngine;

namespace Scriptables
{
    [CreateAssetMenu(menuName = "Mad Slime/Levels Catalog", fileName = "NewLevelsCatalog")]
    public sealed class LevelsCatalog : ScriptableObject
    {
        [SerializeField] private List<LevelRange> _ranges = new List<LevelRange>();

        public IReadOnlyList<LevelRange> Ranges => _ranges;
    }
}
