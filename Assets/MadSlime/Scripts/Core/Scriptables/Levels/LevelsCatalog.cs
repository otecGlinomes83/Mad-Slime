using System.Collections.Generic;
using UnityEngine;

namespace Scriptables
{
    [CreateAssetMenu(menuName = "Mad Slime/Levels Catalog", fileName = "NewLevelsCatalog")]
    public sealed class LevelsCatalog : ScriptableObject
    {
        [Tooltip("Диапазоны уровней: с уровня X по уровень Y используется свой конфиг.")]
        [SerializeField] private List<LevelRange> _ranges = new List<LevelRange>();

        public IReadOnlyList<LevelRange> Ranges => _ranges;
    }
}
