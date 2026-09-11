using System;
using Scriptables;
using UnityEngine;

namespace Game
{
    public static class Localization
    {
        public const string Russian = "ru";
        public const string English = "en";
        public const string Turkish = "tr";

        private static readonly string[] LanguageOrder = { Russian, English, Turkish };

        private static LocalizationTable _table;
        private static string _language = Russian;

        public static event Action LanguageChanged;

        public static string CurrentLanguage => _language;

        public static void Initialize(LocalizationTable table, string savedLanguage)
        {
            if (table == null)
            {
                throw new ArgumentNullException(nameof(table));
            }

            _table = table;
            _language = ResolveLanguage(savedLanguage);
        }

        public static string Get(string key)
        {
            if (_table == null)
            {
                throw new InvalidOperationException(
                    "Localization is not initialized. LocalizationService must exist on the ProjectScope.");
            }

            return _table.Get(key, _language);
        }

        public static void SetLanguage(string language)
        {
            string resolved = ResolveLanguage(language);

            if (resolved == _language)
            {
                return;
            }

            _language = resolved;
            LanguageChanged?.Invoke();
        }

        public static void CycleLanguage()
        {
            int next = (Array.IndexOf(LanguageOrder, _language) + 1) % LanguageOrder.Length;
            SetLanguage(LanguageOrder[next]);
        }

        private static string ResolveLanguage(string language)
        {
            if (string.IsNullOrEmpty(language))
            {
                return FromSystemLanguage();
            }

            for (int i = 0; i < LanguageOrder.Length; i++)
            {
                if (LanguageOrder[i] == language)
                {
                    return language;
                }
            }

            return English;
        }

        private static string FromSystemLanguage()
        {
            switch (Application.systemLanguage)
            {
                case UnityEngine.SystemLanguage.Russian:
                    return Russian;

                case UnityEngine.SystemLanguage.Turkish:
                    return Turkish;

                default:
                    return English;
            }
        }
    }
}
