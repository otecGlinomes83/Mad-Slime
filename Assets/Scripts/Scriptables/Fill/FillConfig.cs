using UnityEngine;

namespace Scriptables
{
    [CreateAssetMenu(menuName = "Mad Slime/Fill Config", fileName = "NewFillConfig")]
    public sealed class FillConfig : ScriptableObject
    {
        [Header("Fill")]
        [Tooltip("Пауза перед первым кубом заливки (с).")]
        [SerializeField, Min(0f)] private float _fillDelay = 0.55f;

        [Tooltip("Базовый интервал вылета кубов (с). Меньше = заливка быстрее.")]
        [SerializeField, Min(0.001f)] private float _spawnInterval = 0.04f;

        [Tooltip("Базовое время полёта куба до ячейки (с).")]
        [SerializeField, Min(0.01f)] private float _flightDuration = 0.5f;

        [Header("Tap Boost")]
        [Tooltip("Интервал вылета кубов после первого тапа (с). Меньше = кубы вылетают чаще.")]
        [SerializeField, Min(0.001f)] private float _boostedSpawnInterval = 0.015f;

        [Tooltip("Время полёта куба после первого тапа (с). Меньше = резче долетает.")]
        [SerializeField, Min(0.01f)] private float _boostedFlightDuration = 0.25f;

        [Header("Tap Instant")]
        [Tooltip("Общее окно вылета оставшихся кубов после второго тапа (с). «Моментально» условно: 0.25 = короткий залп.")]
        [SerializeField, Min(0.05f)] private float _instantFillDuration = 0.25f;

        [Tooltip("Время полёта каждого куба залпа (с). Финал наступит после долёта последнего куба.")]
        [SerializeField, Min(0.01f)] private float _instantFlightDuration = 0.1f;

        [Header("Bonus Wave")]
        [Tooltip("Пауза после долёта квотной волны перед бонусной (с). 0 = без паузы.")]
        [SerializeField, Min(0f)] private float _bonusWaveDelay = 0.35f;

        [Tooltip("Цвет, в который подмешиваются бонусные кубы (заливка сверх квоты).")]
        [SerializeField] private Color _bonusTintColor = Color.black;

        [Tooltip("Сила подмешивания цвета бонусных кубов. 0 = не отличаются от квотных, 1 = полностью цвета оттенка.")]
        [SerializeField, Range(0f, 1f)] private float _bonusTintStrength = 0.3f;

        [Header("Border")]
        [Tooltip("Длительность каскада спавна бордюрных кубов (с).")]
        [SerializeField, Min(0.05f)] private float _borderCascadeDuration = 0.5f;

        [Tooltip("Цвет бордюрных кубов.")]
        [SerializeField] private Color _borderColor = Color.black;

        [Header("Ghost")]
        [Tooltip("Прозрачность призрака формы под заливкой. 0 = невидим.")]
        [SerializeField, Range(0f, 1f)] private float _ghostOpacity = 0.4f;

        [Header("Shape Punch")]
        [Tooltip("Насколько сильно пульсирует вся форма в момент полного заполнения (доля скейла). 0 = без пульса.")]
        [SerializeField, Range(0f, 0.3f)] private float _shapePunchStrength = 0.06f;

        [Tooltip("Длительность пульса формы (с). Больше = волна читается дольше, но затягивает паузу перед Win-окном.")]
        [SerializeField, Min(0.01f)] private float _shapePunchDuration = 0.4f;

        [Tooltip("Количество мелких колебаний в пульсе формы. Больше = дрожже.")]
        [SerializeField, Min(0)] private int _shapePunchVibrato = 10;

        [Tooltip("Мягкость хвоста пульса: 0 = жёстко обрывается, 1 = пружинит.")]
        [SerializeField, Range(0f, 1f)] private float _shapePunchElasticity = 0.3f;

        [Header("Camera Kick")]
        [Tooltip("Насколько отъезжает камера при полном заполнении (градусы поля зрения). 0 = камера остаётся на месте.")]
        [SerializeField, Min(0f)] private float _fovKick = 9f;

        [Tooltip("Длительность отъезда камеры (с). Столько же камера возвращается обратно — суммарный цикл 2x, держи меньше WinDelay.")]
        [SerializeField, Min(0.01f)] private float _fovDuration = 0.6f;

        [Header("Session")]
        [Tooltip("Задержка перед окном победы после полного заполнения (с). Должно вмещать конфетти и цикл отъезда камеры.")]
        [SerializeField, Min(0f)] private float _winDelay = 1.3f;

        public float FillDelay => _fillDelay;
        public float SpawnInterval => _spawnInterval;
        public float FlightDuration => _flightDuration;
        public float BoostedSpawnInterval => _boostedSpawnInterval;
        public float BoostedFlightDuration => _boostedFlightDuration;
        public float InstantFillDuration => _instantFillDuration;
        public float InstantFlightDuration => _instantFlightDuration;
        public float BonusWaveDelay => _bonusWaveDelay;
        public Color BonusTintColor => _bonusTintColor;
        public float BonusTintStrength => _bonusTintStrength;
        public float BorderCascadeDuration => _borderCascadeDuration;
        public Color BorderColor => _borderColor;
        public float GhostOpacity => _ghostOpacity;
        public float ShapePunchStrength => _shapePunchStrength;
        public float ShapePunchDuration => _shapePunchDuration;
        public int ShapePunchVibrato => _shapePunchVibrato;
        public float ShapePunchElasticity => _shapePunchElasticity;
        public float FovKick => _fovKick;
        public float FovDuration => _fovDuration;
        public float WinDelay => _winDelay;
    }
}
