# LevelGenerator

**Слой:** Gameplay · **Код:** `Assets/MadSlime/Scripts/Game/Level/LevelGenerator.cs` (+ `ZoneLayoutPlanner.cs`, `ItemSize.cs`, `LayoutPreviewDrawer.cs`)

Строит весь уровень в `Awake`: тема пола → раскладка → тиры предметов → спавн из пула → квота → подсветка «нюхом».

## Пайплайн Generate()

1. `LevelConfigResolver.GetConfigFor(CurrentLevel)`
2. **PickLayout**: из LayoutsLibrary выбрасывает пустые (warning), случайный из годных; ничего годного → исключение
3. `FloorBounds` → `Mover.SetBounds` — игрок получает границы пола именно здесь
4. Тема: `sharedMaterial` пола из LevelTheme
5. **AssignTiers**: варианты PropSet в диапазоне тиров уровня группируются по префабу; шаффл; первые N префабам гарантированно по одному определению каждого тира (каждый тир представлен на поле), остальным — случайный вариант
6. **SpawnItems**: случайное зеркалирование X/Z раскладки; для каждой SpawnZone — пул префабов чьё назначение тир попадает в зону; spacing = радиус крупнейшего (`ItemSize.GetRadiusXZ` × TierTable scale) × factor (или ручной, минимум 0.5); `ZoneLayoutPlanner.Collect` генерит позиции (Grid / CircleGrid с кольцами / Circle / Scatter rejection sampling с лимитом попыток ×10 — **может молча разместить меньше count**); предметы из [[ItemPool]], `SetDefinition` + `Initialize`, кламп позиции внутрь пола
7. `Physics.SyncTransforms()` — оверлап-детекторы сразу видят предметы
8. [[QuotaGenerator]] → [[LevelProgress]].Reset
9. Перк Smell: все квотовые предметы `SetHighlighted(true, HighlightColor)`

`LayoutPreviewDrawer` — редакторский gizmo-просмотрщик раскладок (детерминированный seed).

## Связи

- [[Конфиги уровней]], [[TierTable]], [[LevelConfigResolver]], [[ItemPool]], [[QuotaGenerator]], [[LevelProgress]], [[PlayerUpgrades]], [[Mover]], [[Детекция и сбор | Collector]] (возврат в пул через `ItemCollected`)

## Слабые места

- `_spawnedItems.Remove(item)` — O(n) на каждый собранный предмет, O(n²) за уровень (`LevelGenerator.cs:115`).
- `OnDisable` отписывается без null-проверки полей (`:108-111`).
- Scatter-зона молча недосчитывает предметы — квота это компенсирует (кламп к заспавненному), но раскладка «на глаз» может быть реже задуманной.
- `Awake` генерирует уровень сразу — любой кривой ассет роняет сцену на старте (fail-fast по канону, но без retry).
