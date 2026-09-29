# UpgradesConfig

**Слой:** Core · **Код:** `Assets/MadSlime/Scripts/Core/Scriptables/Upgrades/UpgradesConfig.cs`

Данные магазина улучшений: 4 ступенчатых апгрейда + 3 перка + параметры их эффектов.

## Состав

- **Апгрейды** (`UpgradeType`: Speed, Appetite, Taste, Metabolism): иконка, `_baseCost` (200) + `_costStep` (150) × текущий уровень, `_valuePerStep` (+10% за ступень), `_maxSteps` (5). Цена ступени: `GetCost(level) = base + step × level`. Уровни хранятся в сейве как `SpeedLevel/AppetiteLevel/TasteLevel/MetabolismLevel`
- **Перки** (`PerkType`: Smell, Adrenaline, Ambitions): одноразовая покупка (3000), бафф навсегда; список купленных — `PurchasedPerks`
- Эффекты перков здесь же: `_highlightColor` (подсветка квотовых предметов для «Нюха»), `_adrenalineThresholdFraction` (0.25 — доля таймера) и `_adrenalineSpeedMultiplier` (1.4)

## Связи

- **Потребители:** [[PlayerUpgrades]] (покупка + выдача эффектов), [[ShopPanel]] (грид вкладки «Улучшения», карточки строит UpgradeItemViewFactory), [[AdrenalineBoost]] (порог/множитель), подсветка квоты через Item.SetHighlighted
- Состояние покупок — [[PlayerProgress]] / сейв

## Слабые места

- `GetUpgrade`/`GetPerk` — линейный поиск + `InvalidOperationException` на каждый запрос (`UpgradesConfig.cs:33-59`); для UI-грида это ок, для хот-путей — аллокации/throw как ветка логики.
- Смысл «valuePerStep» интерпретируется потребителем по-своему для каждого UpgradeType — контракт размыт (доля? множитель? секунды?) и живёт в головах.
