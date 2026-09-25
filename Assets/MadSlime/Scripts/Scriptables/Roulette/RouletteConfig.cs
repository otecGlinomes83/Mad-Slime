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
        [Tooltip("Длительность окна рекламных круток (с). 900 = 15 минут.")]
        [SerializeField, Min(60)] private int _adSpinWindowSeconds = 900;

        [Tooltip("Сколько круток за рекламу разрешено внутри окна.")]
        [SerializeField, Min(1)] private int _adSpinsPerWindow = 2;

        [Tooltip("Период бесплатной крутки (с). 600 = раз в 10 минут.")]
        [SerializeField, Min(60)] private int _freeSpinCooldownSeconds = 600;

        public IReadOnlyList<RouletteSector> Sectors => _sectors;
        public int MainSpinCost => _mainSpinCost;
        public IReadOnlyList<SkinItem> ExclusiveSkins => _exclusiveSkins;
        public int SkinSpinBaseCost => _skinSpinBaseCost;
        public int SkinSpinCostStep => _skinSpinCostStep;
        public int DuplicateCoinsCompensation => _duplicateCoinsCompensation;
        public int AdSpinWindowSeconds => _adSpinWindowSeconds;
        public int AdSpinsPerWindow => _adSpinsPerWindow;
        public int FreeSpinCooldownSeconds => _freeSpinCooldownSeconds;
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
