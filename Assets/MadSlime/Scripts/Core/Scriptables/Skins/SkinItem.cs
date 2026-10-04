using System;
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

        [Tooltip("Уникальный id скина: ключ сейвов. Менять после релиза нельзя.")]
        [SerializeField] private string _id;

        public GameObject Model => _model;

        public Sprite Icon => _icon;

        public SkinRarity Rarity => _rarity;

        public string Id => _id;

        private void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(_id) == true)
            {
                throw new InvalidOperationException(
                    $"{name}: SkinItem requires a non-empty _id. It is the save key of the skin.");
            }
        }
    }
}
