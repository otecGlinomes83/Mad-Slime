using System.Collections.Generic;
using UnityEngine;

namespace Scriptables
{
    [CreateAssetMenu(menuName = "Mad Slime/Player Config", fileName = "NewPlayerConfig")]
    public class PlayerConfig : ScriptableObject
    {
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

        [Tooltip("С какой доли пути предмет начинает сжиматься в ноль. 0.6 = последние 40% пути; "
            + "0.8 = доезжает целым и схлопывается у пасти.")]
        [SerializeField, Range(0.1f, 0.95f)] private float _shrinkStart = 0.6f;

        [Tooltip("Крутость разгона ease-in. 1 = линейно, 2 = мягкий отрыв и разгон, 3+ = предмет зависает и резко всасывается.")]
        [SerializeField, Range(1f, 5f)] private float _absorbEasePower = 2f;

        [Header("Collect Squash")]
        [Tooltip("Длительность полного прохода кривой сквоша (с).")]
        [SerializeField, Min(0.01f)] private float _squashDuration = 0.2f;

        [Tooltip("Кривая сквоша: X — время прохода (0–1), Y — множитель размера (1 = обычный, "
            + "0.85 = вжим до 85%). Форму анимации задаёт целиком.")]
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

        [Header("Movement Squash")]
        [Tooltip("Глубина сжатия по высоте при движении. 0.2 = высота сжимается до 80% и возвращается к исходной.")]
        [SerializeField, Range(0f, 0.5f)] private float _deformMaxStretch = 0.22f;

        [Header("Movement Cycle")]
        [Tooltip("Форма сжатия: X — фаза шага (0–1), Y — глубина сжатия (0–1). 0 — исходный "
            + "размер, 1 — максимальное сжатие. Значение в нуле должно совпадать со значением в единице, иначе цикл щёлкает на стыке.")]
        [SerializeField] private AnimationCurve _crawlStretchCurve = new AnimationCurve(
            new Keyframe(0f, 0f),
            new Keyframe(0.35f, 1f),
            new Keyframe(0.7f, 0.4f),
            new Keyframe(1f, 0f));

        [Tooltip("Частота вдавливания при движении (циклы/с). Не зависит от скорости игрока.")]
        [SerializeField, Min(0.01f)] private float _movementSquashFrequency = 1.6f;

        [Tooltip("Во сколько раз ускоряется вдавливание к концу финального отсчёта.")]
        [SerializeField, Min(1f)] private float _finalSquashFrequencyMultiplier = 2f;

        [Tooltip("Плавность включения и затухания цикла ползка (с). Больше = мягче старт и остановка.")]
        [SerializeField, Min(0.01f)] private float _crawlRampTime = 0.15f;

        [Header("Growth")]
        [Tooltip("Стартовая масса слайма в начале уровня.")]
        [SerializeField, Min(0)] private int _startMass;

        [Tooltip("Пороги тиров: при какой массе открывается тир и что он даёт (масштаб, скорость, отъезд камеры).")]
        [SerializeField] private List<PlayerTierThreshold> _thresholds = new List<PlayerTierThreshold>();

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
        public AnimationCurve CrawlStretchCurve => _crawlStretchCurve;
        public float MovementSquashFrequency => _movementSquashFrequency;
        public float FinalSquashFrequencyMultiplier => _finalSquashFrequencyMultiplier;
        public float CrawlRampTime => _crawlRampTime;
        public int StartMass => _startMass;
        public IReadOnlyList<PlayerTierThreshold> Thresholds => _thresholds;
    }
}
