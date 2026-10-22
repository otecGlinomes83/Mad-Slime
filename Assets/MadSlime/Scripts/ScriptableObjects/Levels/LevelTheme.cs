using System.Collections.Generic;
using UnityEngine;

namespace Scriptables
{
    [CreateAssetMenu(menuName = "Mad Slime/Level Theme", fileName = "NewLevelTheme")]
    public class LevelTheme : ScriptableObject
    {
        [Tooltip("Материал пола уровня.")]
        [SerializeField] private Material _floorMaterial;

        [SerializeField] private Material _backgroundMaterial;
        [SerializeField] private Material _edgeMaterial;
        [SerializeField, Min(1f)] private float _platformDepth = 12f;
        [SerializeField] private GameObject _environmentPrefab;

        [Tooltip("Текстура формы для сцены заливки: альфа задаёт силуэт, цвета проступают в призраке и конфетти.")]
        [SerializeField] private Texture2D _fillShapeTexture;

        [Tooltip("Случайная картинка для каждой заливки. При пустом списке используется основная текстура формы.")]
        [SerializeField] private List<Texture2D> _fillShapeTextures = new List<Texture2D>();

        public Material FloorMaterial => _floorMaterial;
        public Material BackgroundMaterial => _backgroundMaterial;
        public Material EdgeMaterial => _edgeMaterial;
        public float PlatformDepth => _platformDepth;
        public GameObject EnvironmentPrefab => _environmentPrefab;
        public Texture2D FillShapeTexture => _fillShapeTexture;

        public Texture2D PickFillShapeTexture()
        {
            if (_fillShapeTextures.Count == 0)
            {
                return _fillShapeTexture;
            }

            return _fillShapeTextures[Random.Range(0, _fillShapeTextures.Count)];
        }
    }
}
