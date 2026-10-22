# QuotaEntry

**Слой:** Core · **Код:** `Assets/MadSlime/Scripts/Quota/Data/QuotaEntry.cs`

Одна строка квоты уровня: какой предмет, сколько нужно, сколько собрано. `[Serializable]`, рантайм-счётчик `_collected` не сериализуется.

## Зачем

Квота уровня = список QuotaEntry. Генерирует его [[Квота | QuotaGenerator]] из параметров [[Конфиги уровней | LevelConfig]], живёт список в `QuotaBoard`, счёт тикает через `TryRegisterCollected`, а `QuotaCompleted` завершает сессию ([[GameplayOrchestrator]]).

## Связи

- Создаётся: [[Квота | QuotaGenerator]]
- Читается: `QuotaBoard`/`QuotaCounter`, HUD ([[HUD | QuotaUI]]), подсветка/призраки ([[Детекция и сбор | QuotaItemHighlighter, GhostToggler]])
