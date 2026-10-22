# UpgradesConfig

**Слой:** Core · **Код:** `Assets/MadSlime/Scripts/ScriptableObjects/Upgrades/UpgradesConfig.cs`

Конфиг прогрессии магазина: апгрейды, перки, подсветка.

## Состав

- `Upgrades: List<UpgradeEntry>` — `UpgradeType` (Speed/Appetite/Taste/Metabolism), иконка, base/step цены, value за ступень, число ступеней (5)
- `Perks: List<PerkEntry>` — `PerkType` (Smell/Adrenaline/Ambitions), цена, `Value` (например множитель адреналина)
- `HighlightColor` + `OutlineWidth` — подсветка квотных предметов перком Smell ([[Предметы | GhostAnimator]])

Потребители: [[PlayerUpgrades]] (множители + магазин), [[Магазин | UpgradesPagePresenter]] (грид), [[Квота | QuotaCounter]] (Metabolism), [[Рост игрока | CollectAvailability]] (Ambitions), [[AdrenalineBoost]].

Компаньоны `UpgradeEntry`/`PerkEntry` живут в файле SO — канон проекта ([[AI_RULES]]).
