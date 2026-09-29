# QuotaEntry

**Слой:** Core · **Код:** `Assets/MadSlime/Scripts/Core/QuotaEntry.cs`

Одна строка квоты уровня: какой предмет, сколько нужно, сколько собрано. `[Serializable]`, рантайм-счётчик `_collected` не сериализуется.

## Зачем

Квота уровня = список QuotaEntry. Генерирует его [[QuotaGenerator]] из параметров [[Конфиги уровней | LevelConfig]], живёт список в [[LevelProgress]], прогресс тикает [[Player]] через `LevelProgress.RegisterCollected()`, а `QuotaCompleted` завершает сессию ([[GameplaySessionHandler]]).

## Связи

- Создаётся: [[QuotaGenerator]]
- Читается: [[LevelProgress]], HUD ([[HUD]] — тарелки QuotaUI), [[Детекция и сбор | ItemGhostToggler]] / подсветка

## Слабые места

- Конструктор валидирует (`definition != null`, `targetCount >= 0`) — это хорошо; но сам список квот никто не проверяет на пустоту до старта сессии.
