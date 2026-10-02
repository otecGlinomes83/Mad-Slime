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
        [Tooltip("Цвет аутлайна квотовых предметов (Улучшенный нюх). Уходит в _OtlColor шейдера MadSlime/ItemOutlineSToon.")]
        [SerializeField] private Color _highlightColor = new Color(1f, 0.85f, 0.2f, 1f);

        [Tooltip("Ширина аутлайна квотовых предметов — значение _OtlWidth шейдера ItemOutlineSToon: " +
            "силуэт раздувается по нормалям на 0.008 * ширина * масштаб предмета. " +
            "1 ≈ едва заметная линия, 5 ≈ жирная обводка. Настраивать ТУТ: материал QuotaHighlight перекрывается из конфига.")]
        [SerializeField, Min(0f)] private float _outlineWidth = 4f;

        [Header("Adrenaline")]
        [Tooltip("Доля таймера, ниже которой включается Адреналин. 0.25 = последние 25% времени.")]
        [SerializeField, Range(0.01f, 0.9f)] private float _adrenalineThresholdFraction = 0.25f;

        public IReadOnlyList<UpgradeEntry> Upgrades => _upgrades;
        public IReadOnlyList<PerkEntry> Perks => _perks;
        public Color HighlightColor => _highlightColor;
        public float OutlineWidth => _outlineWidth;
        public float AdrenalineThresholdFraction => _adrenalineThresholdFraction;

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

        [Tooltip("Прирост эффекта за КАЖДУЮ ступень отдельно (доля от 1), по порядку. " +
            "Число значений обязано совпадать с MaxSteps.")]
        [SerializeField, Min(0f)] private List<float> _stepValues = new List<float>();

        [Tooltip("Максимальное число ступеней.")]
        [SerializeField, Min(1)] private int _maxSteps = 5;

        public UpgradeType Type => _type;
        public Sprite Icon => _icon;
        public int BaseCost => _baseCost;
        public int CostStep => _costStep;
        public IReadOnlyList<float> StepValues => _stepValues;
        public int MaxSteps => _maxSteps;

        public int GetCost(int currentLevel)
        {
            return _baseCost + _costStep * currentLevel;
        }

        public float GetTotalValue(int level)
        {
            float total = 0f;

            for (int i = 0; i < level && i < _stepValues.Count; i++)
            {
                total += _stepValues[i];
            }

            return total;
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

        [Tooltip("Сколько даёт перк. Амбиции — прибавка к тиру притягиваемых предметов. " +
            "Адреналин — множитель скорости. Нюх — значение не использует.")]
        [SerializeField, Min(0f)] private float _value;

        public PerkType Type => _type;
        public Sprite Icon => _icon;
        public int Cost => _cost;
        public float Value => _value;
    }
}
