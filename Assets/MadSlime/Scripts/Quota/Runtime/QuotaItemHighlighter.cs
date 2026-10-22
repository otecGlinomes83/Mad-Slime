using Collectables;
using Items;
using Player;
using Quota;
using Skills;
using System.Collections.Generic;
using Unity.Profiling;
using UnityEngine;
using VContainer;

namespace Game
{
    public class QuotaItemHighlighter : MonoBehaviour
    {
        private ProfilerMarker _refreshMarker = new ProfilerMarker("QuotaItemHighlighter.Refresh");
        private ProfilerMarker _evaluateTypesMarker = new ProfilerMarker("QuotaItemHighlighter.EvaluateTypes");
        private ProfilerMarker _applyChangesMarker = new ProfilerMarker("QuotaItemHighlighter.ApplyChanges");

        [SerializeField] private Color _highlightColor = new Color(1f, 0.85f, 0.2f, 1f);
        [SerializeField, Min(0f)] private float _outlineWidth = 4f;

        private QuotaItemDetector _detector;
        private QuotaBoard _board;
        private CollectAvailability _collectAvailability;
        private PlayerTier _playerTier;
        private Dictionary<ItemDefinition, QuotaTypeState> _quotaTypes =
            new Dictionary<ItemDefinition, QuotaTypeState>();
        private Dictionary<Item, QuotaTrackedItem> _items = new Dictionary<Item, QuotaTrackedItem>();
        private HashSet<Item> _pendingItems = new HashSet<Item>();
        private bool _hasDirtyTypes;

        public Color HighlightColor => _highlightColor;
        public float OutlineWidth => _outlineWidth;

        [Inject]
        public void Construct(QuotaItemDetector detector, QuotaBoard board, CollectAvailability collectAvailability,
            PlayerTier playerTier)
        {
            _detector = detector;
            _board = board;
            _collectAvailability = collectAvailability;
            _playerTier = playerTier;
        }

        private void OnEnable()
        {
            foreach (QuotaEntry entry in _board.Entries)
            {
                _quotaTypes.Add(entry.Definition, new QuotaTypeState(entry, IsHighlightable(entry)));
            }

            _detector.Detected += OnDetected;
            _detector.Exited += OnExited;
            _board.QuotaChanged += OnQuotaChanged;
            _playerTier.TierChanged += OnTierChanged;
        }

        private void OnDisable()
        {
            _detector.Detected -= OnDetected;
            _detector.Exited -= OnExited;
            _board.QuotaChanged -= OnQuotaChanged;
            _playerTier.TierChanged -= OnTierChanged;

            foreach (QuotaTrackedItem item in _items.Values)
            {
                if (item.Animator != null)
                {
                    item.Animator.SetHighlighted(false);
                }
            }

            _items.Clear();
            _pendingItems.Clear();
            _quotaTypes.Clear();
            _hasDirtyTypes = false;
        }

        private void OnDetected(Item item)
        {
            if (item == null || _items.ContainsKey(item)
                || _quotaTypes.TryGetValue(item.Definition, out QuotaTypeState type) == false)
            {
                return;
            }

            if (item.TryGetComponent(out GhostAnimator animator) == false
                || item.TryGetComponent(out ItemBody body) == false)
            {
                return;
            }

            _items.Add(item, new QuotaTrackedItem(animator, body.Collider, type));
            _pendingItems.Add(item);
        }

        private void OnExited(Item item)
        {
            if (ReferenceEquals(item, null) || _items.TryGetValue(item, out QuotaTrackedItem tracked) == false)
            {
                return;
            }

            _items.Remove(item);
            _pendingItems.Remove(item);

            if (tracked.Animator != null)
            {
                tracked.Animator.SetHighlighted(false);
            }
        }

        private void OnQuotaChanged(int remaining, QuotaEntry entry)
        {
            if (remaining > 0 || _quotaTypes.TryGetValue(entry.Definition, out QuotaTypeState type) == false)
            {
                return;
            }

            type.MarkDirty();
            _hasDirtyTypes = true;
        }

        private void OnTierChanged(SizeTier previousTier, SizeTier currentTier)
        {
            foreach (QuotaTypeState type in _quotaTypes.Values)
            {
                type.MarkDirty();
            }

            _hasDirtyTypes = true;
        }

        private void LateUpdate()
        {
            if (_hasDirtyTypes == false && _pendingItems.Count == 0)
            {
                return;
            }

            using ProfilerMarker.AutoScope refreshScope = _refreshMarker.Auto();

            EvaluateTypes();
            ApplyChanges();
            ClearChanges();
        }

        private void EvaluateTypes()
        {
            using ProfilerMarker.AutoScope evaluateTypesScope = _evaluateTypesMarker.Auto();

            foreach (QuotaTypeState type in _quotaTypes.Values)
            {
                if (type.IsDirty == false)
                {
                    continue;
                }

                bool isHighlighted = IsHighlightable(type.Entry);
                type.UpdateHighlight(isHighlighted);
            }
        }

        private void ApplyChanges()
        {
            using ProfilerMarker.AutoScope applyChangesScope = _applyChangesMarker.Auto();

            foreach (KeyValuePair<Item, QuotaTrackedItem> pair in _items)
            {
                QuotaTrackedItem item = pair.Value;

                if (item.Type.HasChanged == false && _pendingItems.Contains(pair.Key) == false)
                {
                    continue;
                }

                if (item.Animator == null)
                {
                    continue;
                }

                bool isHighlighted = item.Type.IsHighlighted && item.Collider != null
                    && item.Collider.enabled && item.Animator.isActiveAndEnabled;
                item.Animator.SetHighlighted(isHighlighted);
            }
        }

        private void ClearChanges()
        {
            foreach (QuotaTypeState type in _quotaTypes.Values)
            {
                type.ClearChanges();
            }

            _pendingItems.Clear();
            _hasDirtyTypes = false;
        }

        private bool IsHighlightable(QuotaEntry entry)
        {
            return entry.GetRemaining() > 0 && _collectAvailability.CanCollect(entry.Definition.Tier);
        }
    }
}
