using System.Collections.Generic;
using Quota;
using UnityEngine;

namespace UI
{
    public sealed class QuotaUI : MonoBehaviour
    {
        [SerializeField] private QuotaTracker _quotaTracker;
        [SerializeField] private QuotaPlateUI _platePrefab;
        [SerializeField] private RectTransform _container;
        [SerializeField] private float _verticalSpacing = 60f;

        private readonly List<QuotaPlateUI> _plates = new List<QuotaPlateUI>();
        private readonly Dictionary<QuotaEntry, QuotaPlateUI> _platesByEntry = new Dictionary<QuotaEntry, QuotaPlateUI>();
        private int _nextPlateIndex;

        private void OnEnable()
        {
            _quotaTracker.QuotaChanged += OnQuotaChanged;
        }

        private void OnDisable()
        {
            _quotaTracker.QuotaChanged -= OnQuotaChanged;
        }

        private void OnQuotaChanged(int remaining, QuotaEntry entry)
        {
            if (TryGetPlate(entry, out QuotaPlateUI plate) == false)
            {
                plate = CreatePlate(entry);
            }

            plate.UpdateCount(remaining);
        }

        private bool TryGetPlate(QuotaEntry entry, out QuotaPlateUI plate)
        {
            return _platesByEntry.TryGetValue(entry, out plate);
        }

        private QuotaPlateUI CreatePlate(QuotaEntry entry)
        {
            QuotaPlateUI newPlate = Instantiate(_platePrefab, _container);
            newPlate.transform.localPosition = new Vector3(0f, -_nextPlateIndex * _verticalSpacing, 0f);

            newPlate.Setup(entry);

            _plates.Add(newPlate);
            _platesByEntry[entry] = newPlate;
            _nextPlateIndex++;

            return newPlate;
        }
    }
}
