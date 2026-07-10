using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(menuName = "Mad Slime/Shop Content", fileName = "NewShopContent")]
public class ShopContent : ScriptableObject
{
    [SerializeField] private List<SkinItem> _skinItems;

    public IEnumerable<SkinItem> SkinItems => _skinItems;

    private void OnValidate()
    {
        var skinDuplikates = _skinItems
            .Where(item => item != null)
            .GroupBy(item => item.SkinType)
            .Where(array => array.Count() > 1);

        if (skinDuplikates.Count() > 0)
        {
            throw new InvalidOperationException("Duplicate SkinType" + skinDuplikates.First().Key);
        }
    }
}