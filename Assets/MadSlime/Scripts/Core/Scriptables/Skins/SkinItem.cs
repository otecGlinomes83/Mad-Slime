using Player;
using UnityEngine;

namespace Skins
{
    [CreateAssetMenu(menuName = "Mad Slime/Shop Item", fileName = "NewShopItem")]
    public sealed class SkinItem : ScriptableObject
    {
        [Tooltip("Модель скина, инстансится на игрока при выборе.")]
        [SerializeField] private GameObject _model;

        [Tooltip("Иконка скина в магазине.")]
        [SerializeField] private Sprite _icon;

        [Tooltip("Редкость скина: задаёт цвет плашки и вес выпадения в скин-рулетке.")]
        [SerializeField] private SkinRarity _rarity;

        [Tooltip("Цена в монетах. 0 = выдаётся бесплатно.")]
        [SerializeField, Range(0, 10000)] private int _price;

        [Tooltip("Уникальный тип скина; дубли в ShopContent запрещены.")]
        [SerializeField] private PlayerSkins _skinType;

        public GameObject Model => _model;

        public Sprite Icon => _icon;

        public SkinRarity Rarity => _rarity;

        public int Price => _price;

        public PlayerSkins SkinType => _skinType;
    }
}
