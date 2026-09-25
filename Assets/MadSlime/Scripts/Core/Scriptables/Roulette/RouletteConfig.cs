using System;
using System.Collections.Generic;
using Skins;
using UnityEngine;

namespace Roulette
{
    [CreateAssetMenu(menuName = "Mad Slime/Roulette Config", fileName = "NewRouletteConfig")]
    public sealed class RouletteConfig : ScriptableObject
    {
        [Header("Main Roulette")]
        [Tooltip("Секторы основной рулетки: монеты и эксклюзивные скины. Вес сектора задаёт его шанс.")]
        [SerializeField] private List<RouletteSector> _sectors = new List<RouletteSector>();

        [Tooltip("Цена платной крутки основной рулетки (константа).")]
        [SerializeField, Min(0)] private int _mainSpinCost = 100;

        [Tooltip("Эксклюзивные скины: нельзя купить, можно только выиграть. Показываются в магазине.")]
        [SerializeField] private List<SkinItem> _exclusiveSkins = new List<SkinItem>();

        [Header("Skin Roulette")]
        [Tooltip("Базовая цена крутки скин-рулетки.")]
        [SerializeField, Min(0)] private int _skinSpinBaseCost = 300;

        [Tooltip("Насколько дорожает каждая следующая крутка скин-рулетки. Цена не сбрасывается никогда.")]
        [SerializeField, Min(0)] private int _skinSpinCostStep = 150;

        [Tooltip("Компенсация монетами за сектор скина, когда все обычные скины уже собраны.")]
        [SerializeField, Min(0)] private int _duplicateCoinsCompensation = 100;

        [Header("Timers")]
        [Tooltip("Длительность окна рекламных круток (с). 1800 = 30 минут.")]
        [SerializeField, Min(60)] private int _adSpinWindowSeconds = 1800;

        [Tooltip("Сколько круток за рекламу разрешено внутри окна.")]
        [SerializeField, Min(1)] private int _adSpinsPerWindow = 3;

        [Tooltip("Период бесплатной крутки (с). 900 = раз в 15 минут.")]
        [SerializeField, Min(60)] private int _freeSpinCooldownSeconds = 900;

        [Header("Wheel Motion")]
        [Tooltip("Скорость холостого вращения колеса (градусов в секунду).")]
        [SerializeField, Min(0f)] private float _idleDegreesPerSecond = 24f;

        [Tooltip("Резкий откат колеса назад перед круткой (градусов).")]
        [SerializeField, Min(0f)] private float _windBackDegrees = 45f;

        [Tooltip("Длительность отката назад (с).")]
        [SerializeField, Min(0.05f)] private float _windBackDuration = 0.35f;

        [Tooltip("Минимальное число полных оборотов при крутке.")]
        [SerializeField, Min(1)] private int _minTurns = 3;

        [Tooltip("Максимальное число полных оборотов при крутке.")]
        [SerializeField, Min(1)] private int _maxTurns = 5;

        [Tooltip("Длительность основной крутки (с).")]
        [SerializeField, Min(0.5f)] private float _spinDuration = 3f;

        public IReadOnlyList<RouletteSector> Sectors => _sectors;
        public int MainSpinCost => _mainSpinCost;
        public IReadOnlyList<SkinItem> ExclusiveSkins => _exclusiveSkins;
        public int SkinSpinBaseCost => _skinSpinBaseCost;
        public int SkinSpinCostStep => _skinSpinCostStep;
        public int DuplicateCoinsCompensation => _duplicateCoinsCompensation;
        public int AdSpinWindowSeconds => _adSpinWindowSeconds;
        public int AdSpinsPerWindow => _adSpinsPerWindow;
        public int FreeSpinCooldownSeconds => _freeSpinCooldownSeconds;
        public float IdleDegreesPerSecond => _idleDegreesPerSecond;
        public float WindBackDegrees => _windBackDegrees;
        public float WindBackDuration => _windBackDuration;
        public int MinTurns => _minTurns;
        public int MaxTurns => _maxTurns;
        public float SpinDuration => _spinDuration;
    }

    [Serializable]
    public sealed class RouletteSector
    {
        public enum RewardKind
        {
            Coins = 0,
            Skin
        }

        [Tooltip("Тип награды сектора: монеты или скин.")]
        [SerializeField] private RewardKind _rewardKind = RewardKind.Coins;

        [Tooltip("Количество монет (для типа Coins).")]
        [SerializeField, Min(0)] private int _coins = 100;

        [Tooltip("Эксклюзивный скин (для типа Skin). Берётся из списка эксклюзивных.")]
        [SerializeField] private SkinItem _skin;

        [Tooltip("Вес сектора: во сколько раз он вероятнее сектора с весом 1.")]
        [SerializeField, Min(0f)] private float _weight = 1f;

        public RewardKind RewardType => _rewardKind;
        public int Coins => _coins;
        public SkinItem Skin => _skin;
        public float Weight => _weight;
    }
}
