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

        private void OnDisable()
        {
            _quotaTracker.QuotaChanged -= OnQuotaChanged;
        }

        private void Awake()
        {
            _quotaTracker.QuotaChanged += OnQuotaChanged;

            IReadOnlyList<QuotaEntry> entries = _quotaTracker.Entries;

            for (int i = 0; i < entries.Count; i++)
            {
                QuotaEntry entry = entries[i];

                QuotaPlateUI plate = Instantiate(_platePrefab, _container);
                plate.transform.localPosition = new Vector3(0f, -i * _verticalSpacing, 0f);
                plate.Setup(entry);

                _plates.Add(plate);
            }

            _quotaTracker.ReportCurrentStates();
        }

        private void OnQuotaChanged(int remaining, QuotaEntry entry)
        {
            for (int i = 0; i < _plates.Count; i++)
            {
                QuotaPlateUI plate = _plates[i];

                if (plate.Entry != entry)
                {
                    continue;
                }

                plate.UpdateCount(remaining);
                return;
            }
        }
    }
}
