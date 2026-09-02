using System.Collections.Generic;
using Items;
using UnityEngine;

namespace Scriptables
{
    [CreateAssetMenu(menuName = "Mad Slime/Level Theme", fileName = "NewLevelTheme")]
    public sealed class LevelTheme : ScriptableObject
    {
        [SerializeField] private Material _floorMaterial;
        [SerializeField] private List<Item> _itemPool = new List<Item>();
        [SerializeField] private Texture2D _fillShapeTexture;
        [SerializeField] private AudioClip _music;

        public Material FloorMaterial => _floorMaterial;
        public IReadOnlyList<Item> ItemPool => _itemPool;
        public Texture2D FillShapeTexture => _fillShapeTexture;
        public AudioClip Music => _music;
    }
}
