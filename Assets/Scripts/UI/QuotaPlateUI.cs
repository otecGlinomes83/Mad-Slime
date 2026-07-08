using Quota;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public sealed class QuotaPlateUI : MonoBehaviour
    {
        [SerializeField] private Image _icon;
        [SerializeField] private TMP_Text _text;

        private QuotaEntry _entry;

        public QuotaEntry Entry => _entry;

        public void Setup(QuotaEntry entry)
        {
            _entry = entry;
            _icon.sprite = entry.Definition.Icon;
        }

        public void UpdateCount(int remaining)
        {
            _text.text = remaining.ToString();
        }
    }
}