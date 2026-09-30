using System;
using System.Collections.Generic;
using UnityEngine;

namespace Upgrades
{
    [CreateAssetMenu(menuName = "Mad Slime/Upgrades Config", fileName = "NewUpgradesConfig")]
    public sealed class UpgradesConfig : ScriptableObject
    {
        [Tooltip("Ступенчатые прокачки: цена ступени = BaseCost + CostStep * текущий уровень ступени.")]
        [SerializeField] private List<UpgradeEntry> _upgrades = new List<UpgradeEntry>();

        [Tooltip("Одноразовые покупки: стоят дорого, покупаются один раз, бафф навсегда.")]
        [SerializeField] private List<PerkEntry> _perks = new List<PerkEntry>();

        [Header("Smell")]
        [Tooltip("Цвет аутлайна квотовых предметов (Улучшенный нюх).")]
        [SerializeField] private Color _highlightColor = new Color(1f, 0.85f, 0.2f, 1f);

        [Tooltip("Толщина аутлайна квотовых предметов — доля ПОЛОВИНЫ высоты экрана (0.012 ≈ 6px на 1080p), одинаковая по всем направлениям.")]
        [SerializeField, Min(0f)] private float _highlightThickness = 0.012f;

        [Header("Adrenaline")]
        [Tooltip("Доля таймера, ниже которой включается Адреналин. 0.25 = последние 25% времени.")]
        [SerializeField, Range(0.01f, 0.9f)] private float _adrenalineThresholdFraction = 0.25f;

        [Tooltip("Множитель скорости под Адреналином.")]
        [SerializeField, Min(1f)] private float _adrenalineSpeedMultiplier = 1.4f;

        public IReadOnlyList<UpgradeEntry> Upgrades => _upgrades;
        public IReadOnlyList<PerkEntry> Perks => _perks;
        public Color HighlightColor => _highlightColor;
        public float HighlightThickness => _highlightThickness;
        public float AdrenalineThresholdFraction => _adrenalineThresholdFraction;
        public float AdrenalineSpeedMultiplier => _adrenalineSpeedMultiplier;

        public UpgradeEntry GetUpgrade(UpgradeType type)
        {
            for (int i = 0; i < _upgrades.Count; i++)
            {
                if (_upgrades[i].Type == type)
                {
                    return _upgrades[i];
                }
            }

            throw new InvalidOperationException(
                $"UpgradesConfig '{name}': no entry for upgrade '{type}'. Add a row for it.");
        }

        public PerkEntry GetPerk(PerkType type)
        {
            for (int i = 0; i < _perks.Count; i++)
            {
                if (_perks[i].Type == type)
                {
                    return _perks[i];
                }
            }

            throw new InvalidOperationException(
                $"UpgradesConfig '{name}': no entry for perk '{type}'. Add a row for it.");
        }
    }

    [Serializable]
    public sealed class UpgradeEntry
    {
        [Tooltip("Тип ступенчатой прокачки.")]
        [SerializeField] private UpgradeType _type;

        [Tooltip("Иконка прокачки на плашке.")]
        [SerializeField] private Sprite _icon;

        [Tooltip("Цена первой ступени.")]
        [SerializeField, Min(0)] private int _baseCost = 200;

        [Tooltip("Насколько дороже каждая следующая ступень.")]
        [SerializeField, Min(0)] private int _costStep = 150;

        [Tooltip("Прирост эффекта за ступень (доля от 1). 0.1 = +10% за ступень.")]
        [SerializeField, Min(0f)] private float _valuePerStep = 0.1f;

        [Tooltip("Максимальное число ступеней.")]
        [SerializeField, Min(1)] private int _maxSteps = 5;

        public UpgradeType Type => _type;
        public Sprite Icon => _icon;
        public int BaseCost => _baseCost;
        public int CostStep => _costStep;
        public float ValuePerStep => _valuePerStep;
        public int MaxSteps => _maxSteps;

        public int GetCost(int currentLevel)
        {
            return _baseCost + _costStep * currentLevel;
        }
    }

    [Serializable]
    public sealed class PerkEntry
    {
        [Tooltip("Тип одноразовой покупки.")]
        [SerializeField] private PerkType _type;

        [Tooltip("Иконка перка на плашке.")]
        [SerializeField] private Sprite _icon;

        [Tooltip("Цена. Одноразовые покупки стоят очень дорого.")]
        [SerializeField, Min(0)] private int _cost = 3000;

        public PerkType Type => _type;
        public Sprite Icon => _icon;
        public int Cost => _cost;
    }
}
