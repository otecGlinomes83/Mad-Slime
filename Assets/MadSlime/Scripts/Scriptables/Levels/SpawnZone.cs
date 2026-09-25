using Skills;
using UnityEngine;

namespace Scriptables
{
    [System.Serializable]
    public sealed class SpawnZone
    {
        [Tooltip("Форма зоны раскладки: Grid (сетка), Circle (круг), Scatter (хаотичный россыпь), CircleGrid (кольцо из сетки).")]
        [SerializeField] private SpawnShape _shape = SpawnShape.Circle;

        [Tooltip("Центр зоны в координатах комнаты.")]
        [SerializeField] private Vector2 _center = Vector2.zero;

        [Tooltip("Радиус зоны (юниты).")]
        [SerializeField] private float _radius = 3f;

        [Tooltip("Сколько предметов спавнится в зоне.")]
        [SerializeField] private int _count = 8;

        [Tooltip("Вкл — предметы расставляются равномерно автоматически; выкл — используется ручной шаг ниже.")]
        [SerializeField] private bool _autoSpacing = true;

        [Tooltip("Ручной шаг между предметами (юниты), если авторасстояние выключено. 0 = авто.")]
        [SerializeField] private float _spacing = 0f;

        [Tooltip("Минимальный тир предметов в зоне.")]
        [SerializeField] private ItemTier _minTier = ItemTier.Small;

        [Tooltip("Максимальный тир предметов в зоне.")]
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
