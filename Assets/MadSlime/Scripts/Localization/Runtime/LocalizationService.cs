using Core;
using Scriptables;
using System;
using UnityEngine;
using VContainer;

namespace Game
{
    public sealed class LocalizationService : MonoBehaviour
    {
        [SerializeField] private LocalizationTable _table;

        private PlayerProgress _progress;
        private ILanguageProvider _languageProvider;
        private bool _suppressPersist;

        [Inject]
        public void Construct(PlayerProgress progress, ILanguageProvider languageProvider)
        {
            _progress = progress;
            _languageProvider = languageProvider;
        }

        private void Awake()
        {
            if (_table == null)
            {
                throw new InvalidOperationException(
                    $"{name}: LocalizationTable is not assigned. Drag the Localization asset into the _table field.");
            }
        }

        private void OnEnable()
        {
            if (_progress == null || _languageProvider == null)
            {
                throw new InvalidOperationException(
                    $"{name}: dependencies were not injected. Check that ProjectScope is the first root object of the project.");
            }

            Localization.Initialize(_table, _progress.Language);
            Localization.LanguageChanged += OnLanguageChanged;
            _progress.Ready += OnSdkData;
            _languageProvider.LanguageSwitched += OnYandexLangChanged;
            ApplyLanguage();
        }

        private void OnDisable()
        {
            Localization.LanguageChanged -= OnLanguageChanged;
            _progress.Ready -= OnSdkData;
            _languageProvider.LanguageSwitched -= OnYandexLangChanged;
        }

        private void ApplyLanguage()
        {
            string savedLanguage = _progress.Language;

            if (string.IsNullOrEmpty(savedLanguage) == false)
            {
                Localization.SetLanguage(savedLanguage);
                return;
            }

            _suppressPersist = true;
            Localization.SetLanguage(_languageProvider.Language);
            _suppressPersist = false;
        }

        private void OnSdkData()
        {
            ApplyLanguage();
        }

        private void OnYandexLangChanged(string language)
        {
            if (string.IsNullOrEmpty(_progress.Language) == false)
            {
                return;
            }

            _suppressPersist = true;
            Localization.SetLanguage(language);
            _suppressPersist = false;
        }

        private void OnLanguageChanged()
        {
            if (_suppressPersist)
            {
                return;
            }

            _progress.Language = Localization.CurrentLanguage;
            _progress.Save();
        }
    }
}
