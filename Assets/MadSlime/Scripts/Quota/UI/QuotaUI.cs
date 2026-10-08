using System;
using System.Collections.Generic;
using DG.Tweening;
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

        [Tooltip("Длительность выезда первой плашки (с).")]
        [SerializeField, Min(0.01f)] private float _plateIntroDuration = 0.25f;

        [Tooltip("Насколько дольше выезжает каждая следующая плашка (с).")]
        [SerializeField, Min(0f)] private float _plateIntroDurationStep = 0.1f;

        [Tooltip("С какого расстояния слева плашка выезжает (юниты).")]
        [SerializeField, Min(0f)] private float _plateIntroSlideOffset = 90f;

        [SerializeField, Min(0.01f)] private float _shiftDuration = 0.25f;
        [SerializeField, Min(0.01f)] private float _removeDuration = 0.2f;

        private readonly List<QuotaPlateUI> _plates = new List<QuotaPlateUI>();
        private readonly Dictionary<QuotaEntry, QuotaPlateUI> _platesByEntry = new Dictionary<QuotaEntry, QuotaPlateUI>();

        private LevelProgress _levelProgress;
        private bool _isSubscribed;

        [Inject]
        public void Construct(LevelProgress levelProgress)
        {
            _levelProgress = levelProgress;
        }

        private void Awake()
        {
            if (_platePrefab == null)
            {
                throw new InvalidOperationException(
                    $"{name}: PlatePrefab is not assigned. Drag a QuotaPlateUI prefab into the _platePrefab field.");
            }

            if (_container == null)
            {
                throw new InvalidOperationException(
                    $"{name}: Container is not assigned. Drag a RectTransform into the _container field.");
            }
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
                QuotaPlateUI plate = CreatePlate(quota[i], i);
                plate.UpdateCount(quota[i].Remaining);
            }
        }

        private void OnQuotaChanged(int remaining, QuotaEntry entry)
        {
            if (_platesByEntry.TryGetValue(entry, out QuotaPlateUI plate) == false)
            {
                plate = CreatePlate(entry, _plates.Count);
            }

            if (remaining > 0)
            {
                plate.UpdateCount(remaining);
                return;
            }

            RemovePlate(plate);
        }

        private QuotaPlateUI CreatePlate(QuotaEntry entry, int index)
        {
            QuotaPlateUI newPlate = Instantiate(_platePrefab, _container);

            Vector2 finalPosition = new Vector2(0f, -index * _verticalSpacing);
            Vector2 startPosition = finalPosition + new Vector2(-_plateIntroSlideOffset, 0f);
            float duration = _plateIntroDuration + index * _plateIntroDurationStep;

            newPlate.Setup(entry);
            newPlate.PlayIntro(startPosition, finalPosition, duration);

            _plates.Insert(index, newPlate);
            _platesByEntry[entry] = newPlate;

            return newPlate;
        }

        private void RemovePlate(QuotaPlateUI plate)
        {
            _plates.Remove(plate);
            _platesByEntry.Remove(plate.Entry);

            plate.PlayRemoval(_removeDuration, ShiftPlates);
        }

        private void ShiftPlates()
        {
            for (int i = 0; i < _plates.Count; i++)
            {
                _plates[i].MoveTo(new Vector3(0f, -i * _verticalSpacing, 0f), _shiftDuration);
            }
        }
    }
}
