using Skills;
using System.Collections.Generic;
using UnityEngine;

namespace Scriptables
{
    [CreateAssetMenu(menuName = "Mad Slime/Level Config", fileName = "NewLevelConfig")]
    public sealed class LevelConfig : ScriptableObject
    {
        [Tooltip("Тема уровня: материал пола и текстура формы для сцены заливки.")]
        [SerializeField] private LevelTheme _theme;

        [Tooltip("Набор предметов и их вариантов для генерации уровня.")]
        [SerializeField] private PropSet _propSet;

        [Tooltip("Минимальный тир предметов, спавнящихся на уровне.")]
        [SerializeField, Min(0)] private int _minTier;

        [Tooltip("Максимальный тир предметов на уровне.")]
        [SerializeField, Min(0)] private int _maxTier = 3;

        [Tooltip("Длительность таймера уровня (с).")]
        [SerializeField] private float _timerDuration = 90f;

        [Header("Quota Generation")]
        [Tooltip("Минимум разных типов предметов в квоте.")]
        [SerializeField] private int _quotaTypesMin = 1;

        [Tooltip("Максимум разных типов предметов в квоте.")]
        [SerializeField] private int _quotaTypesMax = 3;

        [Tooltip("Минимальное количество предметов по одному типу квоты.")]
        [SerializeField] private int _quotaTargetMin = 3;

        [Tooltip("Максимальное количество предметов по одному типу квоты.")]
        [SerializeField] private int _quotaTargetMax = 8;

        [Tooltip("Сколько типов квоты может принадлежать одному тиру.")]
        [SerializeField] private int _quotaMaxSameTier = 2;

        public LevelTheme Theme => _theme;
        public PropSet PropSet => _propSet;
        public ItemTier MinTier => (ItemTier)Mathf.Clamp(_minTier, 0, (int)ItemTier.Boss);
        public ItemTier MaxTier => (ItemTier)Mathf.Clamp(_maxTier, _minTier, (int)ItemTier.Boss);
        public float TimerDuration => _timerDuration;
        public int QuotaTypesMin => _quotaTypesMin;
        public int QuotaTypesMax => _quotaTypesMax;
        public int QuotaTargetMin => Mathf.Max(1, _quotaTargetMin);
        public int QuotaTargetMax => Mathf.Max(QuotaTargetMin, _quotaTargetMax);
        public int QuotaMaxSameTier => Mathf.Max(1, _quotaMaxSameTier);
    }
}
