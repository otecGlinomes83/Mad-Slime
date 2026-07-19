using UnityEngine;

namespace Skins
{
    public sealed class SkinItemViewFactory : MonoBehaviour
    {
        [SerializeField] private ShopItemView shopItemViewPrefab;

        public ShopItemView Get(SkinItem skinItem, Transform parent)
        {
            ShopItemView instance = Instantiate(shopItemViewPrefab, parent);
            instance.Initialize(skinItem);

            return instance;
        }
    }
}