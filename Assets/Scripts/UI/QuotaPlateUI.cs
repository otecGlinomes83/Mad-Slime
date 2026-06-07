using Quota;
using TMPro;
using UnityEngine;

namespace UI
{
    public sealed class QuotaPlateUI : MonoBehaviour
    {
        [SerializeField] private TMP_Text _text;

        private QuotaEntry _entry;

        public QuotaEntry Entry => _entry;

        public void Setup(QuotaEntry entry)
        {
            _entry = entry;
        }

        public void UpdateCount(int remaining)
        {
            _text.text = $"{_entry.Definition.DisplayName}: {remaining}";
        }
    }
}
