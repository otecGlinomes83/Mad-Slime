using System;
using Player;
using Skins;
using UnityEngine;
using YG;

public class SkinApplier : MonoBehaviour
{
    [SerializeField] private ShopContent _shopContent;
    [SerializeField] private Transform _skinsContainer;

    private GameObject _currentModel;

    private void Awake()
    {
        if (_shopContent == null)
        {
            throw new InvalidOperationException(
                $"{name}: SkinApplier requires _shopContent to be assigned in the inspector.");
        }

        if (_skinsContainer == null)
        {
            throw new InvalidOperationException(
                $"{name}: SkinApplier requires _skinsContainer to be assigned in the inspector.");
        }

        ApplySelectedSkin();
    }

    private void OnDestroy()
    {
        if (_currentModel != null)
        {
            Destroy(_currentModel);
        }
    }

    private void ApplySelectedSkin()
    {
        PlayerSkins selectedType = YG2.saves.SelectedSkinType;
        SkinItem matchingItem = FindItem(selectedType);

        if (matchingItem == null)
        {
            return;
        }

        _currentModel = Instantiate(matchingItem.Model, _skinsContainer);
    }

    private SkinItem FindItem(PlayerSkins skinType)
    {
        foreach (SkinItem item in _shopContent.SkinItems)
        {
            if (item == null)
            {
                continue;
            }

            if (item.SkinType == skinType)
            {
                return item;
            }
        }

        return null;
    }
}
