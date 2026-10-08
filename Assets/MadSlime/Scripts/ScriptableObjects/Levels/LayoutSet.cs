using System.Collections.Generic;
using UnityEngine;

namespace Scriptables
{
    [CreateAssetMenu(menuName = "Mad Slime/Layout Set", fileName = "NewLayoutSet")]
    public sealed class LayoutSet : ScriptableObject
    {
        [Tooltip("Зоны спавна предметов уровня.")]
        [SerializeField] private List<SpawnZone> _zones = new List<SpawnZone>();

        [Tooltip("Разрешить зеркалирование раскладки при генерации — удваивает вариативность.")]
        [SerializeField] private bool _allowMirroring = true;

        [Header("Item Placement")]
        [Tooltip("Шаг авторасстояния между предметами = радиус крупнейшего предмета × этот множитель. Больше = предметы реже.")]
        [SerializeField] private float _autoSpacingFactor = 2.2f;

        [Tooltip("Минимальная дистанция между предметами = шаг × этот множитель. Меньше = плотнее рой.")]
        [SerializeField] private float _scatterDistanceFactor = 0.7f;

        public IReadOnlyList<SpawnZone> Zones => _zones;
        public bool AllowMirroring => _allowMirroring;
        public float AutoSpacingFactor => _autoSpacingFactor;
        public float ScatterDistanceFactor => _scatterDistanceFactor;
    }
}
