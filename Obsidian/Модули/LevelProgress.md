# LevelProgress

**Слой:** Gameplay · **Код:** `Assets/MadSlime/Scripts/Game/Level/LevelProgress.cs` (plain-класс, синглтон [[ProjectLifetimeScope]])

Модель прогресса раунда — и **мост Game → Fill**: живёт в корневом скоупе, поэтому квота раунда переживает выгрузку сцены Game и читается [[ShapeFillOrchestrator | FillCounter]] в Fill. `Reset` вызывается только в [[LevelGenerator]] при входе в Game.

## Состояние и API

- `_quota: List<QuotaEntry>` + `_collectedQuotaCount`, `_totalQuotaTarget` (сумма таргетов), `_collectedDefaultFill` (вес «посторонних» предметов), `_isQuotaCompleted`
- `RegisterCollected(definition, foreignFillWeight)`:
  - предмет в квоте и строка не полна → `entry.RegisterCollected()`, `QuotaChanged(remaining, entry)`
  - сверх квоты или не-квотовый → `_collectedDefaultFill += foreignFillWeight`
  - `foreignFillWeight <= 0` → исключение
- `QuotaCompleted` — один раз при `_collectedQuotaCount >= _totalQuotaTarget`
- `FillPercent = (quotaCount + foreignFill) / totalTarget` — **этот процент и есть «заливка»**, которую доносит в Fill [[ShapeFillOrchestrator | FillCounter]]

## События и кто слушает

- `QuotaChanged` → [[HUD | QuotaUI]]
- `QuotaCompleted` → [[GameplaySessionHandler]] (конец раунда), [[UiAnimations | UiEnableScheduler]] (уборка HUD)
- `ItemCollected` → [[Камера | CameraImpulse]]; `IsQuotaItem` → [[Player]] (множитель массы)

## Слабые места

- Состояние переносится неявно: никто не «отдаёт» его Fill-сцене — просто синглтон живёт дольше сцены. Хрупко при изменении порядка сцен.
- `Reset` только со стороны LevelGenerator: войти в Fill без Game нельзя (нет такого флоу), но контракт нигде не закреплён.
