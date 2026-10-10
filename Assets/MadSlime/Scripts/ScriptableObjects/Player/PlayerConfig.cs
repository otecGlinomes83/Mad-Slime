using System.Collections.Generic;
using UnityEngine;

namespace Scriptables
{
    [CreateAssetMenu(menuName = "Mad Slime/Player Config", fileName = "NewPlayerConfig")]
    public class PlayerConfig : ScriptableObject
    {
        [Header("Movement")]
        [Tooltip("Скорость поворота модели слайма в сторону движения (град/с).")]
        [SerializeField, Min(1f)] private float _rotationSpeed = 420f;

        [Tooltip("Плавность разгона и торможения (с). Больше = инертнее и мягче, меньше = резче отклик.")]
        [SerializeField, Min(0.01f)] private float _moveSmoothTime = 0.12f;

        [Header("Absorption")]
        [Tooltip("Минимальное время полёта предмета в пасть (с). Предметы рядом не всасываются быстрее.")]
        [SerializeField, Min(0.05f)] private float _minAbsorbDuration = 0.1f;

        [Tooltip("Максимальное время полёта предмета (с). Дальние предметы не едут дольше.")]
        [SerializeField, Min(0.05f)] private float _maxAbsorbDuration = 0.45f;

        [Tooltip("Скорость втягивания (юнитов/с). Длительность = дистанция/скорость, зажатая между min и max. Больше = быстрее сбор.")]
        [SerializeField, Min(0.5f)] private float _absorbSpeed = 9f;

        [Tooltip("Минимальная дуга траектории, доля дистанции. 0 = предметы летят по прямой.")]
        [SerializeField, Range(0f, 0.5f)] private float _minArcFraction = 0.1f;

        [Tooltip("Максимальная дуга траектории, доля дистанции. Больше = сильнее завихрение; выше ~0.5 предмет залетает за спину.")]
        [SerializeField, Range(0f, 0.5f)] private float _maxArcFraction = 0.3f;

        [Tooltip("Минимальная скорость вращения предмета в полёте (град/с).")]
        [SerializeField, Min(0f)] private float _minSpinSpeed = 120f;

        [Tooltip("Максимальная скорость вращения предмета (град/с). Оба поля в 0 = без вращения.")]
        [SerializeField, Min(0f)] private float _maxSpinSpeed = 540f;

        [Tooltip("Порог скорости (юниты/с), выше которого слайм дымит. На поздних тирах скорость переваливает за порог. 0 = без дыма.")]
        [SerializeField, Min(0f)] private float _smokeSpeedThreshold;

        [Tooltip("С какой доли пути предмет начинает сжиматься в ноль. 0.6 = последние 40% пути; 0.8 = доезжает целым и схлопывается у пасти.")]
        [SerializeField, Range(0.1f, 0.95f)] private float _shrinkStart = 0.6f;

        [Tooltip("Крутость разгона ease-in. 1 = линейно, 2 = мягкий отрыв и разгон, 3+ = предмет зависает и резко всасывается.")]
        [SerializeField, Range(1f, 5f)] private float _absorbEasePower = 2f;

        [Header("Collect Squash")]
        [Tooltip("Длительность полного прохода кривой сквоша (с).")]
        [SerializeField, Min(0.01f)] private float _squashDuration = 0.2f;

        [Tooltip("Кривая сквоша: X — время прохода (0–1), Y — множитель размера (1 = обычный, 0.85 = вжим до 85%). Форму анимации задаёт целиком.")]
        [SerializeField] private AnimationCurve _squashCurve = new AnimationCurve(
            new Keyframe(0f, 1f),
            new Keyframe(0.35f, 0.88f),
            new Keyframe(1f, 1f));

        [Header("Collect Sound")]
        [Tooltip("Минимальная пауза между звуками сбора (с).")]
        [SerializeField, Min(0f)] private float _pickupSoundMinInterval = 0.05f;

        [Tooltip("Максимальная пауза между звуками сбора (с).")]
        [SerializeField, Min(0f)] private float _pickupSoundMaxInterval = 0.1f;

        [Tooltip("Минимальный питч звука сбора.")]
        [SerializeField, Range(0.1f, 3f)] private float _pickupSoundMinPitch = 0.95f;

        [Tooltip("Максимальный питч звука сбора.")]
        [SerializeField, Range(0.1f, 3f)] private float _pickupSoundMaxPitch = 1.15f;

        [Header("Deform")]
        [Tooltip("Максимальное вытягивание слайма вдоль движения на полной скорости (доля). 0.2 = +20% по оси движения.")]
        [SerializeField, Range(0f, 0.5f)] private float _deformMaxStretch = 0.22f;

        [Tooltip("Насколько сжимаются поперечные оси при вытягивании. 0.5 = половина от величины вытягивания.")]
        [SerializeField, Range(0f, 1f)] private float _deformSqueeze = 0.55f;

        [Header("Crawl")]
        [Tooltip("Форма вытягивания за цикл ползка: X — фаза цикла (0–1), Y — доля от Deform Max Stretch (можно ниже нуля — вжим вдоль движения). Значение в нуле должно совпадать со значением в единице, иначе цикл щёлкает на стыке.")]
        [SerializeField] private AnimationCurve _crawlStretchCurve = new AnimationCurve(
            new Keyframe(0f, 0.2f),
            new Keyframe(0.3f, 1f),
            new Keyframe(0.65f, 0.35f),
            new Keyframe(1f, 0.2f));

        [Tooltip("Множитель скорости за цикл ползка: X — фаза цикла (0–1), Y — множитель (1 = обычная скорость). Держи среднее по циклу около единицы, иначе уедет фактическая скорость. Значение в нуле должно совпадать со значением в единице.")]
        [SerializeField] private AnimationCurve _crawlThrustCurve = new AnimationCurve(
            new Keyframe(0f, 1f),
            new Keyframe(0.3f, 1.45f),
            new Keyframe(0.65f, 0.6f),
            new Keyframe(1f, 1f));

        [Tooltip("Базовая длина шага ползка: сколько метров пути занимает полный цикл.")]
        [SerializeField, Min(0f)] private float _crawlStride = 3f;

        [Tooltip("Прирост длины шага на единицу скорости (с). Шаг = база + скорость × значение. Чем больше, тем ближе частота ползка к константе на больших тирах (предел частоты = 1 / значение).")]
        [SerializeField, Min(0f)] private float _stridePerSpeed = 0.28f;

        [Tooltip("Глубина рывка: насколько сильно скорость пульсирует внутри цикла. 0 = ровная скорость, остаётся только анимация ползка; 1 = максимальные рывки.")]
        [SerializeField, Range(0f, 1f)] private float _crawlThrustDepth = 0.55f;

        [Tooltip("Плавность включения и затухания цикла ползка (с). Больше = мягче старт и остановка.")]
        [SerializeField, Min(0.01f)] private float _crawlRampTime = 0.15f;

        [Header("Growth")]
        [Tooltip("Стартовая масса слайма в начале уровня.")]
        [SerializeField, Min(0)] private int _startMass;

        [Tooltip("Пороги тиров: при какой массе открывается тир и что он даёт (масштаб, скорость, отъезд камеры).")]
        [SerializeField] private List<PlayerTierThreshold> _thresholds = new List<PlayerTierThreshold>();

        public float RotationSpeed => _rotationSpeed;
        public float MoveSmoothTime => _moveSmoothTime;
        public float MinAbsorbDuration => _minAbsorbDuration;
        public float MaxAbsorbDuration => _maxAbsorbDuration;
        public float AbsorbSpeed => _absorbSpeed;
        public float MinArcFraction => _minArcFraction;
        public float MaxArcFraction => _maxArcFraction;
        public float MinSpinSpeed => _minSpinSpeed;
        public float MaxSpinSpeed => _maxSpinSpeed;
        public float SmokeSpeedThreshold => _smokeSpeedThreshold;
        public float ShrinkStart => _shrinkStart;
        public float AbsorbEasePower => _absorbEasePower;
        public float SquashDuration => _squashDuration;
        public AnimationCurve SquashCurve => _squashCurve;
        public float PickupSoundMinInterval => _pickupSoundMinInterval;
        public float PickupSoundMaxInterval => _pickupSoundMaxInterval;
        public float PickupSoundMinPitch => _pickupSoundMinPitch;
        public float PickupSoundMaxPitch => _pickupSoundMaxPitch;
        public float DeformMaxStretch => _deformMaxStretch;
        public float DeformSqueeze => _deformSqueeze;
        public AnimationCurve CrawlStretchCurve => _crawlStretchCurve;
        public AnimationCurve CrawlThrustCurve => _crawlThrustCurve;
        public float CrawlStride => _crawlStride;
        public float StridePerSpeed => _stridePerSpeed;
        public float CrawlThrustDepth => _crawlThrustDepth;
        public float CrawlRampTime => _crawlRampTime;
        public int StartMass => _startMass;
        public IReadOnlyList<PlayerTierThreshold> Thresholds => _thresholds;
    }
}
