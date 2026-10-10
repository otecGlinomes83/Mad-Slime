using System;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Shop
{
    public class UpgradeItemViewFactory : MonoBehaviour
    {
        [SerializeField] private UpgradeItemView _upgradeItemViewPrefab;

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

        public UpgradeItemView Get(Transform parent)
        {
            return _resolver.Instantiate(_upgradeItemViewPrefab, parent);
        }
    }
}
