using System;
using UnityEngine;
using Upgrades;
using VContainer;
using VContainer.Unity;

namespace Skins
{
    public sealed class UpgradeItemViewFactory : MonoBehaviour
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

        public UpgradeItemView Get(PlayerUpgrades upgrades, UpgradeType type, Transform parent)
        {
            UpgradeItemView instance = _resolver.Instantiate(_upgradeItemViewPrefab, parent);
            instance.Initialize(upgrades, type);

            return instance;
        }

        public UpgradeItemView Get(PlayerUpgrades upgrades, PerkType type, Transform parent)
        {
            UpgradeItemView instance = _resolver.Instantiate(_upgradeItemViewPrefab, parent);
            instance.Initialize(upgrades, type);

            return instance;
        }
    }
}
