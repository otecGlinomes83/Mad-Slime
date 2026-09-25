using UnityEngine;

namespace Scriptables
{
    [CreateAssetMenu(menuName = "Mad Slime/Level Theme", fileName = "NewLevelTheme")]
    public sealed class LevelTheme : ScriptableObject
    {
        [Tooltip("Материал пола уровня.")]
        [SerializeField] private Material _floorMaterial;

        [Tooltip("Текстура формы для сцены заливки: альфа задаёт силуэт, цвета проступают в призраке и конфетти.")]
        [SerializeField] private Texture2D _fillShapeTexture;

        public Material FloorMaterial => _floorMaterial;
        public Texture2D FillShapeTexture => _fillShapeTexture;
    }
}
