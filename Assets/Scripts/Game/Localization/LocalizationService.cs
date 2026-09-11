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
        private YandexEnvironmentBridge _environmentBridge;

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

            _environmentBridge = YandexEnvironmentBridge.Create(transform);
            _environmentBridge.LangReceived += OnYandexLangReceived;
            _environmentBridge.PlayerIdReceived += OnYandexPlayerIdReceived;
        }

        private void OnEnable()
        {
            Localization.LanguageChanged += OnLanguageChanged;
            _environmentBridge.RequestLanguage();
            _environmentBridge.RequestPlayerId();
        }

        private void OnDisable()
        {
            Localization.LanguageChanged -= OnLanguageChanged;
            _environmentBridge.LangReceived -= OnYandexLangReceived;
            _environmentBridge.PlayerIdReceived -= OnYandexPlayerIdReceived;
        }

        private void OnYandexLangReceived(string language)
        {
            if (string.IsNullOrEmpty(YG2.saves.Language) == false)
            {
                return;
            }

            Localization.SetLanguage(language);
        }

        private void OnYandexPlayerIdReceived(string playerId)
        {
            if (string.IsNullOrEmpty(playerId))
            {
                return;
            }

            string savedId = _progress.PlayerId;

            if (string.IsNullOrEmpty(savedId) || savedId.StartsWith(PlayerProgress.GuestIdPrefix) == true)
            {
                _progress.PlayerId = playerId;
                _progress.Save();
            }
        }

        private void OnLanguageChanged()
        {
            _progress.Language = Localization.CurrentLanguage;
            _progress.Save();
        }
    }
}
