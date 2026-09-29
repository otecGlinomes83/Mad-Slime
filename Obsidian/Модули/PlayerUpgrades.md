# PlayerUpgrades

**Слой:** Gameplay · **Код:** `Assets/MadSlime/Scripts/Upgrades/PlayerUpgrades.cs` (MonoBehaviour, префаб [[ProjectLifetimeScope]])

Фасад прогрессии: переводит уровни апгрейдов из сейва + [[UpgradesConfig]] в числовые множители, которые читает геймплей.

## Производные значения

| Свойство | Формула | Кто читает |
|---|---|---|
| `SpeedMultiplier` | 1 + 0.1 × уровень Speed | [[Mover]]/[[LevelScaler]] |
| `MassMultiplier` | по Appetite | [[Player]] (масса за любой предмет) |
| `QuotaMassMultiplier` | Appetite × Taste вместе | [[Player]] (масса за квотный предмет) |
| `ForeignFillMultiplier` | по Metabolism | [[Player]] (вес «заливки» не-квотой) |
| `HasSmell` / `HasAdrenaline` / `HasAmbitions` | PurchasedPerks.Contains | подсветка квоты, [[AdrenalineBoost]], |
| `AmbitionTierOffset` | HasAmbitions ? 1 : 0 | порог «не по зубам» ([[Детекция и сбор]]) |
| `AdrenalineSpeedMultiplier/ThresholdFraction`, `HighlightColor` | из конфига | AdrenalineBoost, подсветка |

## Магазин

`GetLevel`, `IsMaxed`, `GetNextCost` (base + step × level), `PurchaseStepped` (уровень+1 ⇒ Save), `GetPerkCost`, `PurchasePerk` (Add ⇒ Save). `Awake` прогоняет все записи конфига через геттеры — проверка, что сейв согласован.

## Связи

- [[UpgradesConfig]], [[PlayerProgress]], потребители из таблицы + [[ShopPanel]] (грид вкладки «Улучшения»)

## Слабые места

- Семантика «valuePerStep» у каждого типа своя (доля/множитель) — интерпретация живёт в этом классе, контракт неявный.
- Двойная роль: и рантайм-множители, и логика магазина — смешение ответственности в одном классе.
