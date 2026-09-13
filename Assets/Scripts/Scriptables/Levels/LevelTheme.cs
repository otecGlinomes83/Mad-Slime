using UnityEngine;

namespace Scriptables
{
    [CreateAssetMenu(menuName = "Mad Slime/Level Theme", fileName = "NewLevelTheme")]
    public sealed class LevelTheme : ScriptableObject
    {
        [SerializeField] private Material _floorMaterial;
        [SerializeField] private Texture2D _fillShapeTexture;

        public Material FloorMaterial => _floorMaterial;
        public Texture2D FillShapeTexture => _fillShapeTexture;
    }
}
