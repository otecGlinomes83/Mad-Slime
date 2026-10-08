using System;
using System.Collections.Generic;
using Skins;
using UnityEngine;

namespace Shop
{
    [CreateAssetMenu(menuName = "Mad Slime/Shop Content", fileName = "NewShopContent")]
    public sealed class ShopContent : ScriptableObject
    {
        [Tooltip("Список скинов, доступных в магазине. Дубли и пустые id запрещены.")]
        [SerializeField] private List<SkinItem> _skinItems;

        public IEnumerable<SkinItem> SkinItems => _skinItems;

        private void OnValidate()
        {
            for (int i = 0; i < _skinItems.Count; i++)
            {
                SkinItem item = _skinItems[i];

                if (item == null)
                {
                    throw new InvalidOperationException(
                        $"{name}: SkinItems[{i}] is null. Remove empty slots from the list.");
                }

                for (int j = i + 1; j < _skinItems.Count; j++)
                {
                    SkinItem other = _skinItems[j];

                    if (other == null)
                    {
                        continue;
                    }

                    if (other.Id == item.Id)
                    {
                        throw new InvalidOperationException(
                            $"{name}: duplicate skin id '{item.Id}' at SkinItems[{i}] and SkinItems[{j}].");
                    }
                }
            }
        }
    }
}
