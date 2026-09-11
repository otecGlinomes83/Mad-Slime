using UnityEngine;

namespace Skins
{
    public sealed class ShopItemViewFactory : MonoBehaviour
    {
        [SerializeField] private ShopItemView _shopItemViewPrefab;

        public ShopItemView Get(SkinItem skinItem, Transform parent)
        {
            ShopItemView instance = Instantiate(_shopItemViewPrefab, parent);
            instance.Initialize(skinItem);

            return instance;
        }
    }
}
