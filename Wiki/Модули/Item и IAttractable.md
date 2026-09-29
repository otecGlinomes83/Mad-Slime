# Item и IAttractable

**Слой:** Core · **Код:** `Assets/MadSlime/Scripts/Core/Item.cs`, `IAttractable.cs`

`Item` — MonoBehaviour предмета на полу; `IAttractable` — минимальный контракт для магнита и детекторов (`Tier`, `Self`).

## Зачем

Предмет — пассивные данные + визуальные состояния (обычный / призрак / подсвеченный), без знания о игроке и квоте. Кто и когда его ест — решают [[Детекция и сбор]] и [[Player]].

## Как работает

- `Definition` (`ItemDefinition`): иконка для HUD-квоты + `ItemTier` (Small/Medium/Large/Boss)
- `SetGhost(bool)` — свап материалов на dither-шейдер `MadSlime/GhostDither`, плавный фейд `_Opacity` через DOTween (`SetLink(gameObject, KillOnDisable)`), длительность/ease из GhostFadeConfig. Призрак = предмет слишком высокого тира (выше текущего тира игрока + перк Ambitions), подошедший вплотную — есть нельзя, см. [[Детекция и сбор | ItemGhostToggler]] в [[Детекция и сбор]]
- `SetHighlighted(bool, color)` — подсветка квотовых предметов (перк «Нюх»), цвет из UpgradesConfig, через MaterialPropertyBlock
- `Initialize(pos, scale)` — позиция, случайный поворот по Y, масштаб из TierTable, включение коллайдера
- `Collect()` / `Shutdown()` — выключение коллайдера и деактивация GO (возврат в [[ItemPool]])
- `Awake` — fail-fast: коллайдер, ghost-материал со свойством `_Opacity`, GhostFadeConfig, рендереры

## Связи

- **Спавнится:** [[LevelGenerator]] через [[ItemPool]]
- **Едят:** [[Детекция и сбор]] → [[Player]]
- **Состояние гоняет:** [[Детекция и сбор | ItemGhostToggler]], подсветка — [[PlayerUpgrades]] (перк Smell)

## Слабые места

- `_highlightMaterial` не проверяется в `Awake` (`Item.cs:100-111`) — null вскроется только при первом `SetHighlighted` (`Item.cs:166-170`), несвоевременный fail-fast.
- Поле `_fadeTween` пишется (`Item.cs:152`), но никогда не читается — мёртвое состояние, остановка твига идёт через `DOTween.Kill(this)`.
