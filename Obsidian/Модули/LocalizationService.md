# LocalizationService

**Слой:** Gameplay · **Код:** `Assets/MadSlime/Scripts/Game/Localization/LocalizationService.cs`, `Localization.cs` · префаб [[ProjectLifetimeScope]]

Синхронизирует **статическую** локализацию с сейвом и языком платформы.

## Статический Localization

- Языки: ru / en / tr, цикл кнопкой `CycleLanguage` ([[Локализация UI | LanguageSwitcher]])
- `Get(key)` — из [[Мелкие конфиги Core | LocalizationTable]]; не инициализирован → исключение
- Событие `LanguageChanged` — на него сидит весь UI ([[HUD | LevelLabelUI]], LocalizedText и пр.)
- Системный язык: Russian→ru, Turkish→tr, остальное→en; неизвестный сохранённый → en

## LocalizationService (MonoBehaviour)

- `OnEnable`: `Localization.Initialize(table, сейв)`; подписки: своё событие, `PlayerProgress.Ready` (догрузка SDK), `ILanguageProvider.LanguageSwitched` ([[Интерфейсы Core (шов YG2)]])
- **Приоритет:** сейв непуст → язык игрока; сейв пуст → язык Яндекса с `_suppressPersist` (авто-выбор **не** пишется в сейв)
- В сейв язык пишется **только при ручной смене** игроком

## Слабые места

- Статическое состояние `s_table`/`s_language` переживает сцены и не защищено от повторного `Initialize` с другой таблицей (`Localization.cs:15-16`).
- «Пустая строка в сейве = пользователь ещё не выбирал» — контракт на magic-значении.
- `LocalizationTable` держит собственные хардкоды "en"/"tr" вместо констант Localization — двойной источник истины по кодам языков.
