using Player;
using UnityEngine;

namespace Skins
{
    [CreateAssetMenu(menuName = "Mad Slime/Shop Item", fileName = "NewShopItem")]
    public class SkinItem : ScriptableObject
    {
        [field: Tooltip("Модель скина, инстансится на игрока при выборе.")]
        [field: SerializeField] public GameObject Model { get; private set; }

        [field: Tooltip("Иконка скина в магазине.")]
        [field: SerializeField] public Sprite Icon { get; private set; }

        [field: SerializeField, Range(0, 10000), Tooltip("Цена в монетах. 0 = выдаётся бесплатно.")]
        public int Price { get; private set; }

        [field: Tooltip("Уникальный тип скина; дубли в ShopContent запрещены.")]
        [field: SerializeField] public PlayerSkins SkinType { get; private set; }
    }
}
