using Collectables;
using Items;
using Quota;
using System.Collections.Generic;
using UnityEngine;
using VContainer;

namespace Game
{
    public class QuotaItemHighlighter : MonoBehaviour
    {
        [SerializeField] private Color _highlightColor = new Color(1f, 0.85f, 0.2f, 1f);
        [SerializeField, Min(0f)] private float _outlineWidth = 4f;

        private QuotaItemDetector _detector;
        private QuotaBoard _board;
        private HashSet<Item> _items = new HashSet<Item>();

        [Inject]
        public void Construct(QuotaItemDetector detector, QuotaBoard board)
        {
            _detector = detector;
            _board = board;
        }

        private void OnEnable()
        {
            _detector.Detected += OnDetected;
            _detector.Exited += OnExited;
            _board.QuotaChanged += OnQuotaChanged;
        }

        private void OnDisable()
        {
            _detector.Detected -= OnDetected;
            _detector.Exited -= OnExited;
            _board.QuotaChanged -= OnQuotaChanged;

            foreach (Item item in _items)
            {
                SetHighlighted(item, false);
            }

            _items.Clear();
        }

        private void OnDetected(Item item)
        {
            _items.Add(item);
            SetHighlighted(item, _board.IsQuotaItem(item.Definition));
        }

        private void OnExited(Item item)
        {
            _items.Remove(item);
            SetHighlighted(item, false);
        }

        private void OnQuotaChanged(int remaining, QuotaEntry entry)
        {
            foreach (Item item in _items)
            {
                if (item != null)
                {
                    SetHighlighted(item, _board.IsQuotaItem(item.Definition));
                }
            }
        }

        private void SetHighlighted(Item item, bool isHighlighted)
        {
            if (item != null && item.TryGetComponent(out ItemVisual visual))
            {
                visual.SetHighlighted(isHighlighted, _highlightColor, _outlineWidth);
            }
        }
    }
}
