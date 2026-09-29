# RewardConfig

**Слой:** Core · **Код:** `Assets/MadSlime/Scripts/Core/Scriptables/Rewards/RewardConfig.cs`

Экономика исхода уровня: сколько монет даёт победа и за что платят при провале.

## Параметры

- `_baseReward` = 50 — базовая награда за победу
- `_loseMultiplierThreshold` = 0.25 — минимальная доля заливки при провале, за которую ещё платят; ниже — ноль
- `_loseRewardDivisor` = 4 — при провале базовая награда делится на это

## Связи

- **Единственный потребитель:** [[Rewarder]] (сцена Fill, после [[ShapeFillOrchestrator | FillCounter]])
- Удвоение награды — отдельный rewarded-блок (`YandexConfig._doubleRewardId = "DoubleReward"`, см. [[Мелкие конфиги Core]] и [[AdScheduler]])

## Слабые места

- Формула победы (`baseReward` без множителей за уровень/переполнение) — прогрессия монет линейная; см. [[Техдолг и баги]].
