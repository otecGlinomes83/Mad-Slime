using System;
using UnityEngine;

namespace Quota
{
    public class QuotaPlateSpawner
    {
        private QuotaPlateUI _platePrefab;
        private RectTransform _container;

        public QuotaPlateSpawner(QuotaPlateUI platePrefab, RectTransform container)
        {
            if (platePrefab == null)
            {
                throw new ArgumentNullException(nameof(platePrefab),
                    "QuotaPlateSpawner requires a plate prefab.");
            }

            if (container == null)
            {
                throw new ArgumentNullException(nameof(container),
                    "QuotaPlateSpawner requires a container.");
            }

            _platePrefab = platePrefab;
            _container = container;
        }

        public QuotaPlateUI Spawn(QuotaEntry entry)
        {
            QuotaPlateUI plate = UnityEngine.Object.Instantiate(_platePrefab, _container);

            plate.Setup(entry.Definition.Icon, entry.Remaining);

            return plate;
        }
    }
}
