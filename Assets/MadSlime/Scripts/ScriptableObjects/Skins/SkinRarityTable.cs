using System;
using System.Collections.Generic;
using UnityEngine;

namespace Skins
{
    [CreateAssetMenu(menuName = "Mad Slime/Skin Rarity Table", fileName = "SkinRarityTable")]
    public sealed class SkinRarityTable : ScriptableObject
    {
        [Tooltip("Настройки редкостей: цвета плашек карточек.")]
        [SerializeField] private List<RaritySettings> _settings = new List<RaritySettings>();

        public IReadOnlyList<RaritySettings> Settings => _settings;

        public RaritySettings Get(SkinRarity rarity)
        {
            for (int i = 0; i < _settings.Count; i++)
            {
                if (_settings[i].Rarity == rarity)
                {
                    return _settings[i];
                }
            }

            throw new InvalidOperationException(
                $"{name}: SkinRarityTable has no settings for rarity '{rarity}'.");
        }
    }

    [Serializable]
    public sealed class RaritySettings
    {
        [Tooltip("Редкость, к которой относятся настройки.")]
        [SerializeField] private SkinRarity _rarity = SkinRarity.Common;

        [Tooltip("Цвет плашки карточки скина в альбоме.")]
        [SerializeField] private Color _plateColor = Color.white;

        public SkinRarity Rarity => _rarity;

        public Color PlateColor => _plateColor;
    }
}
