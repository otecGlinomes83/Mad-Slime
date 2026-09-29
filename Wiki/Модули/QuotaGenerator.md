# QuotaGenerator

**Слой:** Gameplay · **Код:** `Assets/MadSlime/Scripts/Game/Level/QuotaGenerator.cs` (plain-класс)

Превращает «что заспавнено» в выполнимую квоту уровня.

## Логика

1. Кандидаты — только типы, которых заспавнено ≥ `QuotaTargetMin` (квота гарантированно собираема)
2. `typesTarget = Random(QuotaTypesMin, QuotaTypesMax+1)`, кламп к числу кандидатов
3. Шаффл кандидатов; при наборе — лимит `QuotaMaxSameTier` типов на один ItemTier (квота не вся из мелочи)
4. `target = Random(QuotaTargetMin, QuotaTargetMax+1)`; если target > заспавнено — warning и кламп вниз

## Связи

- Вход: счётчики спавна [[LevelGenerator]], параметры [[Конфиги уровней | LevelConfig]]
- Выход: `List<QuotaEntry>` в [[LevelProgress]]

## Слабые места

- Квота может выйти меньше запрошенной **молча**: если лимит тиров съедает кандидатов (`QuotaGenerator.cs:27-50`) — сложно диагностировать «почему квота из 1 строки».
