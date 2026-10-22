using System;
using System.Collections.Generic;
using UnityEngine;
using UI.Animations;
using VContainer;

namespace Quota
{
    [RequireComponent(typeof(AnimationScheduler))]
    public class QuotaUI : MonoBehaviour
    {
        [SerializeField] private QuotaPlateUI _platePrefab;
        [SerializeField] private RectTransform _container;
        [SerializeField, Min(1f)] private float _verticalSpacing = 60f;
        [Tooltip("Задержка перед выездом каждой следующей плашки (с).")]
        [SerializeField, Min(0f)] private float _plateIntroDurationStep = 0.1f;
        [SerializeField, Min(0f)] private float _removalSlideOffset = 90f;
        [SerializeField, Min(0.01f)] private float _shiftDuration = 0.25f;
        [SerializeField, Min(0.01f)] private float _removeDuration = 0.2f;

        private List<QuotaPlateUI> _plates = new List<QuotaPlateUI>();
        private List<QuotaPlateAnimator> _animators = new List<QuotaPlateAnimator>();
        private List<QuotaPlateAnimator> _removing = new List<QuotaPlateAnimator>();
        private Dictionary<QuotaEntry, QuotaPlateUI> _platesByEntry = new Dictionary<QuotaEntry, QuotaPlateUI>();
        private QuotaBoard _board;
        private QuotaPlateSpawner _plateSpawner;
        private AnimationScheduler _introSequence;
        private List<Action> _introAnimations = new List<Action>();
        private List<float> _introDelays = new List<float>();

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
                throw new InvalidOperationException($"{name}: AnimationScheduler is required.");
            }

            _plateSpawner = new QuotaPlateSpawner(_platePrefab, _container);
        }

        private void OnEnable()
        {
            _board.QuotaChanged += OnQuotaChanged;
            _board.ResetCompleted += OnQuotaResetCompleted;
            OnQuotaResetCompleted();
        }

        private void OnDisable()
        {
            if (_board != null)
            {
                _board.QuotaChanged -= OnQuotaChanged;
                _board.ResetCompleted -= OnQuotaResetCompleted;
            }

            Clear();
        }

        private void OnQuotaResetCompleted()
        {
            Clear();
            IReadOnlyList<QuotaEntry> entries = _board.Entries;

            for (int i = 0; i < entries.Count; i++)
            {
                QuotaEntry entry = entries[i];

                if (entry.GetRemaining() <= 0)
                {
                    continue;
                }

                QuotaPlateUI plate = _plateSpawner.Spawn(entry);

                if (plate.TryGetComponent(out QuotaPlateAnimator animator) == false)
                {
                    Destroy(plate.gameObject);
                    throw new InvalidOperationException($"{name}: plate requires QuotaPlateAnimator.");
                }

                plate.transform.localPosition = new Vector3(0f, -i * _verticalSpacing, 0f);
                animator.PrepareIntro();
                _introAnimations.Add(animator.PlayIntro);
                float delay = 0f;

                if (_introAnimations.Count > 1)
                {
                    delay = _plateIntroDurationStep;
                }

                _introDelays.Add(delay);
                _plates.Add(plate);
                _animators.Add(animator);
                _platesByEntry.Add(entry, plate);
            }

            if (_plates.Count > 0)
            {
                _introSequence.Play(_introAnimations, _introDelays);
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
            animator.PlayRemoval(_removeDuration, _removalSlideOffset);
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

            _introAnimations.Clear();
            _introDelays.Clear();
            _plates.Clear();
            _animators.Clear();
            _removing.Clear();
            _platesByEntry.Clear();
        }
    }
}
