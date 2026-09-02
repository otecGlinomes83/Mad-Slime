using System;
using System.Collections.Generic;
using Game;
using Quota;
using UnityEngine;
using VContainer;

namespace UI
{
    public sealed class QuotaUI : MonoBehaviour
    {
        [SerializeField] private QuotaPlateUI _platePrefab;
        [SerializeField] private RectTransform _container;
        [SerializeField] private float _verticalSpacing = 60f;

        private readonly List<QuotaPlateUI> _plates = new List<QuotaPlateUI>();
        private readonly Dictionary<QuotaEntry, QuotaPlateUI> _platesByEntry = new Dictionary<QuotaEntry, QuotaPlateUI>();

        private LevelProgress _levelProgress;
        private bool _isSubscribed;

        [Inject]
        public void Construct(LevelProgress levelProgress)
        {
            _levelProgress = levelProgress;
        }

        private void OnEnable()
        {
            SubscribeIfNeeded();
        }

        private void Start()
        {
            if (_levelProgress == null)
            {
                throw new InvalidOperationException(
                    $"{name}: LevelProgress was not injected. Check that GameLifetimeScope is configured and QuotaUI is registered.");
            }

            SubscribeIfNeeded();
            Populate();
        }

        private void OnDisable()
        {
            _isSubscribed = false;

            if (_levelProgress != null)
            {
                _levelProgress.QuotaChanged -= OnQuotaChanged;
            }
        }

        private void SubscribeIfNeeded()
        {
            if (_isSubscribed == true || _levelProgress == null)
            {
                return;
            }

            _isSubscribed = true;
            _levelProgress.QuotaChanged += OnQuotaChanged;
        }

        private void Populate()
        {
            IReadOnlyList<QuotaEntry> quota = _levelProgress.Quota;

            for (int i = 0; i < quota.Count; i++)
            {
                QuotaPlateUI plate = CreatePlate(quota[i]);
                plate.UpdateCount(quota[i].Remaining);
            }
        }

        private void OnQuotaChanged(int remaining, QuotaEntry entry)
        {
            if (_platesByEntry.TryGetValue(entry, out QuotaPlateUI plate) == false)
            {
                plate = CreatePlate(entry);
            }

            plate.UpdateCount(remaining);
        }

        private QuotaPlateUI CreatePlate(QuotaEntry entry)
        {
            QuotaPlateUI newPlate = Instantiate(_platePrefab, _container);
            newPlate.transform.localPosition = new Vector3(0f, -_plates.Count * _verticalSpacing, 0f);

            newPlate.Setup(entry);

            _plates.Add(newPlate);
            _platesByEntry[entry] = newPlate;

            return newPlate;
        }
    }
}
