using Core;
using Saves;
using Scriptables;
using System;
using UnityEngine;
using VContainer;

namespace Game
{
    public class LocalizationService : MonoBehaviour
    {
        [SerializeField] private LocalizationTable _table;

        private ILanguageStorage _languageStorage;
        private ISavesReadiness _savesReadiness;
        private ILanguageProvider _languageProvider;
        private bool _suppressPersist;

        [Inject]
        public void Construct(ILanguageStorage languageStorage, ISavesReadiness savesReadiness,
            ILanguageProvider languageProvider)
        {
            _languageStorage = languageStorage;
            _savesReadiness = savesReadiness;
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
            if (_languageStorage == null || _savesReadiness == null || _languageProvider == null)
            {
                throw new InvalidOperationException(
                    $"{name}: dependencies were not injected. Check that ProjectScope is the first root object of the project.");
            }

            Localization.Initialize(_table, "");
            Localization.LanguageChanged += OnLanguageChanged;
            _savesReadiness.Ready += OnSavesReady;
            _languageProvider.LanguageSwitched += OnYandexLangChanged;
            ApplyLanguage();
        }

        private void OnDisable()
        {
            Localization.LanguageChanged -= OnLanguageChanged;

            if (_savesReadiness != null)
            {
                _savesReadiness.Ready -= OnSavesReady;
            }

            _languageProvider.LanguageSwitched -= OnYandexLangChanged;
        }

        private void ApplyLanguage()
        {
            if (_savesReadiness.IsReady == false)
            {
                SetSuppressedLanguage(_languageProvider.Language);
                return;
            }

            string savedLanguage = _languageStorage.Language;

            if (string.IsNullOrEmpty(savedLanguage) == false)
            {
                Localization.SetLanguage(savedLanguage);
                return;
            }

            SetSuppressedLanguage(_languageProvider.Language);
        }

        private void SetSuppressedLanguage(string language)
        {
            _suppressPersist = true;
            Localization.SetLanguage(language);
            _suppressPersist = false;
        }

        private void OnSavesReady()
        {
            ApplyLanguage();
        }

        private void OnYandexLangChanged(string language)
        {
            if (_savesReadiness.IsReady == true && string.IsNullOrEmpty(_languageStorage.Language) == false)
            {
                return;
            }

            SetSuppressedLanguage(language);
        }

        private void OnLanguageChanged()
        {
            if (_suppressPersist)
            {
                return;
            }

            _languageStorage.SetLanguage(Localization.CurrentLanguage);
        }
    }
}
