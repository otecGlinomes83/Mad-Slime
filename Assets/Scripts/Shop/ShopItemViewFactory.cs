using UnityEngine;

namespace Skins
{
    public sealed class SkinItemViewFactory : MonoBehaviour
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