using System;
using System.Collections.Generic;
using UnityEngine;
using VContainer;

namespace Quota
{
    [RequireComponent(typeof(QuotaIntroSequence))]
    public class QuotaUI : MonoBehaviour
    {
        [SerializeField] private QuotaPlateUI _platePrefab;
        [SerializeField] private RectTransform _container;
        [SerializeField, Min(1f)] private float _verticalSpacing = 60f;
        [SerializeField, Min(0.01f)] private float _plateIntroDuration = 0.25f;
        [Tooltip("Задержка перед выездом каждой следующей плашки (с).")]
        [SerializeField, Min(0f)] private float _plateIntroDurationStep = 0.1f;
        [SerializeField, Min(0f)] private float _plateIntroSlideOffset = 90f;
        [SerializeField, Min(0.01f)] private float _shiftDuration = 0.25f;
        [SerializeField, Min(0.01f)] private float _removeDuration = 0.2f;

        private List<QuotaPlateUI> _plates = new List<QuotaPlateUI>();
        private List<QuotaPlateAnimator> _animators = new List<QuotaPlateAnimator>();
        private List<QuotaPlateAnimator> _removing = new List<QuotaPlateAnimator>();
        private Dictionary<QuotaEntry, QuotaPlateUI> _platesByEntry = new Dictionary<QuotaEntry, QuotaPlateUI>();
        private QuotaBoard _board;
        private QuotaPlateSpawner _plateSpawner;
        private QuotaIntroSequence _introSequence;

        [Inject]
        public void Construct(QuotaBoard board)
        {
            _board = board;
        }

        private void Awake()
        {
            if (_platePrefab == null || _container == null || _board == null)
            {
                throw new InvalidOperationException($"{name}: quota board, plate prefab and container are required.");
            }

            if (TryGetComponent(out _introSequence) == false)
            {
                throw new InvalidOperationException($"{name}: QuotaIntroSequence is required.");
            }

            _plateSpawner = new QuotaPlateSpawner(_platePrefab, _container);
        }

        private void OnEnable()
        {
            _board.QuotaChanged += OnQuotaChanged;
            _board.ResetCompleted += Populate;
            Populate();
        }

        private void OnDisable()
        {
            if (_board != null)
            {
                _board.QuotaChanged -= OnQuotaChanged;
                _board.ResetCompleted -= Populate;
            }

            Clear();
        }

        private void Populate()
        {
            Clear();
            IReadOnlyList<QuotaEntry> entries = _board.Entries;

            for (int i = 0; i < entries.Count; i++)
            {
                QuotaEntry entry = entries[i];

                if (entry.Remaining <= 0)
                {
                    continue;
                }

                QuotaPlateUI plate = _plateSpawner.Spawn(entry);

                if (plate.TryGetComponent(out QuotaPlateAnimator animator) == false)
                {
                    Destroy(plate.gameObject);
                    throw new InvalidOperationException($"{name}: plate requires QuotaPlateAnimator.");
                }

                _plates.Add(plate);
                _animators.Add(animator);
                _platesByEntry.Add(entry, plate);
            }

            if (_plates.Count > 0)
            {
                _introSequence.Play(_animators, _verticalSpacing, _plateIntroSlideOffset,
                    _plateIntroDuration, _plateIntroDurationStep);
            }
        }

        private void OnQuotaChanged(int remaining, QuotaEntry entry)
        {
            if (_platesByEntry.TryGetValue(entry, out QuotaPlateUI plate) == false)
            {
                return;
            }

            int index = _plates.IndexOf(plate);
            QuotaPlateAnimator animator = _animators[index];

            if (remaining > 0)
            {
                plate.UpdateCount(remaining);
                animator.PlayPop();
                return;
            }

            _introSequence.Cancel();
            _platesByEntry.Remove(entry);
            _plates.RemoveAt(index);
            _animators.RemoveAt(index);
            _removing.Add(animator);
            animator.RemovalCompleted += OnRemovalCompleted;
            animator.PlayRemoval(_removeDuration, _plateIntroSlideOffset);
        }

        private void OnRemovalCompleted(QuotaPlateAnimator animator)
        {
            animator.RemovalCompleted -= OnRemovalCompleted;
            _removing.Remove(animator);
            Destroy(animator.gameObject);

            for (int i = 0; i < _animators.Count; i++)
            {
                _animators[i].MoveTo(new Vector3(0f, -i * _verticalSpacing, 0f), _shiftDuration);
            }
        }

        private void Clear()
        {
            if (_introSequence != null)
            {
                _introSequence.Cancel();
            }

            foreach (QuotaPlateAnimator animator in _animators)
            {
                animator.Cancel();
                animator.gameObject.SetActive(false);
                Destroy(animator.gameObject);
            }

            foreach (QuotaPlateAnimator animator in _removing)
            {
                animator.RemovalCompleted -= OnRemovalCompleted;
                animator.Cancel();
                animator.gameObject.SetActive(false);
                Destroy(animator.gameObject);
            }

            _plates.Clear();
            _animators.Clear();
            _removing.Clear();
            _platesByEntry.Clear();
        }
    }
}
