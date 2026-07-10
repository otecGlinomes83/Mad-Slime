using UnityEngine;

namespace Skins
{
    [CreateAssetMenu(menuName = "Mad Slime/Skin Item View Factory", fileName = "NewFactory")]
    public class SkinItemViewFactory : ScriptableObject
    {
        [SerializeField] private SkinItemView _skinItemViewPrefab;

        public SkinItemView Get(SkinItem skinItem, Transform parent)
        {
            SkinItemView instance = Instantiate(_skinItemViewPrefab, parent);
            instance.Initialize(skinItem);

            return instance;
        }
    }
}