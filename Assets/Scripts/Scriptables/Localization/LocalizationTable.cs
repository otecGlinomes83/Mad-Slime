using System;
using System.Collections.Generic;
using UnityEngine;

namespace Scriptables
{
    [CreateAssetMenu(menuName = "Mad Slime/Localization Table", fileName = "Localization")]
    public sealed class LocalizationTable : ScriptableObject
    {
        [SerializeField] private List<LocaleEntry> _entries = new List<LocaleEntry>();

        public string Get(string key, string language)
        {
            for (int i = 0; i < _entries.Count; i++)
            {
                if (_entries[i].Key != key)
                {
                    continue;
                }

                return _entries[i].Get(language);
            }

            return key;
        }

        public IEnumerable<LocaleEntry> Entries => _entries;
    }

    [Serializable]
    public sealed class LocaleEntry
    {
        [SerializeField] private string _key;
        [SerializeField] private string _ru;
        [SerializeField] private string _en;
        [SerializeField] private string _tr;

        public string Key => _key;

        public string Get(string language)
        {
            if (language == "en")
            {
                return Pick(_en, _ru);
            }

            if (language == "tr")
            {
                return Pick(_tr, _en);
            }

            return Pick(_ru, _en);
        }

        private static string Pick(string preferred, string fallback)
        {
            if (string.IsNullOrEmpty(preferred) == false)
            {
                return preferred;
            }

            if (string.IsNullOrEmpty(fallback) == false)
            {
                return fallback;
            }

            return string.Empty;
        }
    }
}
