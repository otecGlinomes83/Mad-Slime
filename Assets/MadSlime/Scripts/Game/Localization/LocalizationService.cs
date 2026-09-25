using System;
using Scriptables;
using UnityEngine;
using VContainer;
using YG;

namespace Game
{
    public sealed class LocalizationService : MonoBehaviour
    {
        [SerializeField] private LocalizationTable _table;

        private PlayerProgress _progress;
        private bool _suppressPersist;

        [Inject]
        public void Construct(PlayerProgress progress)
        {
            _progress = progress;
        }

        private void Awake()
        {
            if (_table == null)
            {
                throw new InvalidOperationException(
                    $"{name}: LocalizationTable is not assigned. Drag the Localization asset into the _table field.");
            }

            Localization.Initialize(_table, YG2.saves.Language);
        }

        private void OnEnable()
        {
            Localization.LanguageChanged += OnLanguageChanged;
            YG2.onGetSDKData += OnSDKData;
            YG2.onSwitchLang += OnYandexLangChanged;
            ApplyLanguage();
        }

        private void OnDisable()
        {
            Localization.LanguageChanged -= OnLanguageChanged;
            YG2.onGetSDKData -= OnSDKData;
            YG2.onSwitchLang -= OnYandexLangChanged;
        }

        private void ApplyLanguage()
        {
            string savedLanguage = YG2.saves.Language;

            if (string.IsNullOrEmpty(savedLanguage) == false)
            {
                Localization.SetLanguage(savedLanguage);
                return;
            }

            _suppressPersist = true;
            Localization.SetLanguage(YG2.lang);
            _suppressPersist = false;
        }

        private void OnSDKData()
        {
            ApplyLanguage();
        }

        private void OnYandexLangChanged(string language)
        {
            if (string.IsNullOrEmpty(YG2.saves.Language) == false)
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
