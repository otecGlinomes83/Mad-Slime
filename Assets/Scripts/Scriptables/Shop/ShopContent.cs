using System;
using System.Collections.Generic;
using System.Linq;
using Player;
using UnityEngine;

namespace Skins
{
    [CreateAssetMenu(menuName = "Mad Slime/Shop Content", fileName = "NewShopContent")]
    public class ShopContent : ScriptableObject
    {
        [SerializeField] private List<SkinItem> _skinItems;

        public IEnumerable<SkinItem> SkinItems => _skinItems;

        private void OnValidate()
        {
            IEnumerable<IGrouping<PlayerSkins, SkinItem>> duplicateGroups = _skinItems
                .Where(item => item != null)
                .GroupBy(item => item.SkinType)
                .Where(group => group.Count() > 1);

            if (duplicateGroups.Count() > 0)
            {
                throw new InvalidOperationException("Duplicate SkinType" + duplicateGroups.First().Key);
            }
        }
    }
}