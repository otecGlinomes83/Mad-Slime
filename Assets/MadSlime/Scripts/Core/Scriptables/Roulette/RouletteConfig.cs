using System;
using System.Collections.Generic;
using Scriptables;
using Skins;
using UnityEngine;
using UnityEngine.Serialization;

namespace Roulette
{
    [CreateAssetMenu(menuName = "Mad Slime/Roulette Config", fileName = "NewRouletteConfig")]
    public sealed class RouletteConfig : ScriptableObject
    {
        [Header("Main Roulette")]
        [Tooltip("Секторы ежедневной рулетки: монеты и эксклюзивные скины. Вес сектора задаёт его шанс.")]
        [SerializeField] private List<RouletteSector> _sectors = new List<RouletteSector>();

        [Tooltip("Эксклюзивные скины: достаются только из этой рулетки, в скин-рулетке магазина не выпадают.")]
        [SerializeField] private List<SkinItem> _exclusiveSkins = new List<SkinItem>();

        [Header("Skin Roulette")]
        [Tooltip("Минимальный порог цены крутки скин-рулетки: с неё цена стартует и ниже не опускается.")]
        [FormerlySerializedAs("_skinSpinBaseCost")]
        [SerializeField, Min(0)] private int _skinSpinMinCost = 300;

        [Tooltip("Максимальный порог цены крутки скин-рулетки. 0 — без предела.")]
        [SerializeField, Min(0)] private int _skinSpinMaxCost = 0;

        [Tooltip("Насколько дорожает каждая следующая крутка скин-рулетки. Цена не сбрасывается никогда.")]
        [SerializeField, Min(0)] private int _skinSpinCostStep = 150;

        [Tooltip("Компенсация монетами за сектор скина, когда все обычные скины уже собраны.")]
        [SerializeField, Min(0)] private int _duplicateCoinsCompensation = 100;

        [Header("Rarity")]
        [Tooltip("Таблица редкостей скинов: веса выпадения и цвета плашек.")]
        [SerializeField] private SkinRarityTable _rarityTable;

        [Header("Timers")]
        [Tooltip("Длительность окна рекламных круток (с). 1800 = 30 минут.")]
        [SerializeField, Min(60)] private int _adSpinWindowSeconds = 1800;

        [Tooltip("Сколько круток за рекламу разрешено внутри окна.")]
        [SerializeField, Min(1)] private int _adSpinsPerWindow = 3;

        [Tooltip("Период бесплатной крутки (с). 1800 = раз в 30 минут.")]
        [SerializeField, Min(60)] private int _freeSpinCooldownSeconds = 1800;

        [Header("Reel Motion")]
        [Tooltip("Пауза между холостыми шагами ленты (с).")]
        [SerializeField, Min(0.1f)] private float _idleStepInterval = 1.1f;

        [Tooltip("Длительность одного холостого шага на одну карточку (с).")]
        [SerializeField, Min(0.05f)] private float _idleStepDuration = 0.18f;

        [Tooltip("Откат ленты назад перед круткой (в карточках).")]
        [SerializeField, Min(0f)] private float _windBackCards = 0.6f;

        [Tooltip("Длительность отката назад (с).")]
        [SerializeField, Min(0.05f)] private float _windBackDuration = 0.3f;

        [Tooltip("Минимальное число полных прокруток всей ленты при крутке.")]
        [SerializeField, Min(1)] private int _minTurns = 3;

        [Tooltip("Максимальное число полных прокруток всей ленты при крутке.")]
        [SerializeField, Min(1)] private int _maxTurns = 5;

        [Tooltip("Длительность основной крутки (с).")]
        [SerializeField, Min(0.5f)] private float _spinDuration = 3.2f;

        [Tooltip("Крутизна торможения: 2 — мягкий разгон и плавный выбег, 5 — классика слотов, 8+ — резкий старт и долгое затухание.")]
        [SerializeField, Min(1f)] private float _spinEasePower = 5f;

        [Tooltip("Пауза ленты на выпавшем призе до попапа выигрыша (с).")]
        [SerializeField, Min(0.1f)] private float _winDwellSeconds = 0.7f;

        [Header("Reel Sounds")]
        [Tooltip("Тик при пролёте карточки. Пусто — лента молчит.")]
        [SerializeField] private SfxClip _stepClip;

        [Tooltip("Звук дёрганья рычага в момент старта крутки. Пусто — без звука.")]
        [SerializeField] private SfxClip _spinStartClip;

        [Tooltip("Фанфара на выпавший приз. Пусто — без звука.")]
        [SerializeField] private SfxClip _winClip;

        public IReadOnlyList<RouletteSector> Sectors => _sectors;
        public IReadOnlyList<SkinItem> ExclusiveSkins => _exclusiveSkins;
        public int SkinSpinMinCost => _skinSpinMinCost;
        public int SkinSpinMaxCost => _skinSpinMaxCost;
        public int SkinSpinCostStep => _skinSpinCostStep;
        public int DuplicateCoinsCompensation => _duplicateCoinsCompensation;
        public SkinRarityTable RarityTable => _rarityTable;
        public int AdSpinWindowSeconds => _adSpinWindowSeconds;
        public int AdSpinsPerWindow => _adSpinsPerWindow;
        public int FreeSpinCooldownSeconds => _freeSpinCooldownSeconds;
        public float IdleStepInterval => _idleStepInterval;
        public float IdleStepDuration => _idleStepDuration;
        public float WindBackCards => _windBackCards;
        public float WindBackDuration => _windBackDuration;
        public int MinTurns => _minTurns;
        public int MaxTurns => _maxTurns;
        public float SpinDuration => _spinDuration;

        public float SpinEasePower => _spinEasePower;
        public float WinDwellSeconds => _winDwellSeconds;
        public SfxClip StepClip => _stepClip;
        public SfxClip SpinStartClip => _spinStartClip;
        public SfxClip WinClip => _winClip;
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
