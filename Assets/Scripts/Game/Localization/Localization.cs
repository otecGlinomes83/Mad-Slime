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

        private static LocalizationTable s_table;
        private static string s_language = Russian;

        public static event Action LanguageChanged;

        public static string CurrentLanguage => s_language;

        public static void Initialize(LocalizationTable table, string savedLanguage)
        {
            if (table == null)
            {
                throw new ArgumentNullException(nameof(table));
            }

            s_table = table;
            s_language = ResolveLanguage(savedLanguage);
        }

        public static string Get(string key)
        {
            if (s_table == null)
            {
                throw new InvalidOperationException(
                    "Localization is not initialized. LocalizationService must exist on the ProjectScope.");
            }

            return s_table.Get(key, s_language);
        }

        public static void SetLanguage(string language)
        {
            string resolved = ResolveLanguage(language);

            if (resolved == s_language)
            {
                return;
            }

            s_language = resolved;
            LanguageChanged?.Invoke();
        }

        public static void CycleLanguage()
        {
            int next = (Array.IndexOf(LanguageOrder, s_language) + 1) % LanguageOrder.Length;
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
