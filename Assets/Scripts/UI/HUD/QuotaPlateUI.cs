using Quota;
using Scriptables;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public sealed class QuotaPlateUI : MonoBehaviour
    {
        [SerializeField] private Image _icon;
        [SerializeField] private TMP_Text _text;
        [SerializeField] private TMP_Text _tierBadge;
        [SerializeField] private TierTable _tierTable;

        private QuotaEntry _entry;

        public QuotaEntry Entry => _entry;

        private void Awake()
        {
            if (_icon == null)
            {
                throw new InvalidOperationException(
                    $"{name}: Icon is not assigned. Drag an Image into the _icon field.");
            }

            if (_text == null)
            {
                throw new InvalidOperationException(
                    $"{name}: Text is not assigned. Drag a TMP_Text into the _text field.");
            }

            if (_tierBadge == null)
            {
                throw new InvalidOperationException(
                    $"{name}: TierBadge is not assigned. Drag a TMP_Text into the _tierBadge field.");
            }

            if (_tierTable == null)
            {
                throw new InvalidOperationException(
                    $"{name}: TierTable is not assigned. Drag the TierTable asset into the _tierTable field.");
            }
        }

        public void Setup(QuotaEntry entry)
        {
            _entry = entry;
            _icon.sprite = entry.Definition.Icon;

            TierEntry tierEntry = _tierTable.Get(entry.Definition.Tier);
            _tierBadge.text = tierEntry.ShortLabel;
            _tierBadge.color = tierEntry.BadgeColor;
        }

        public void UpdateCount(int remaining)
        {
            _text.text = remaining.ToString();
        }
    }
}
