using System;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Skins
{
    public sealed class ShopItemViewFactory : MonoBehaviour
    {
        [SerializeField] private ShopItemView _shopItemViewPrefab;

        private IObjectResolver _resolver;

        [Inject]
        public void Construct(IObjectResolver resolver)
        {
            _resolver = resolver;
        }

        private void Awake()
        {
            if (_resolver == null)
            {
                throw new InvalidOperationException(
                    $"{name}: Resolver was not injected. ShopLifetimeScope must be the first object in the scene hierarchy.");
            }
        }

        public ShopItemView Get(SkinItem skinItem, Transform parent)
        {
            ShopItemView instance = _resolver.Instantiate(_shopItemViewPrefab, parent);
            instance.Initialize(skinItem);

            return instance;
        }
    }
}