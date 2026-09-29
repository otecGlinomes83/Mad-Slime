# AdrenalineBoost

**Слой:** Gameplay · **Код:** `Assets/MadSlime/Scripts/Player/AdrenalineBoost.cs`

Перк **Adrenaline**: ускорение в конце таймера.

## Механика

- Подписка `Timer.Ticked`; перк не куплен → сброс множителя
- `threshold = Timer.Duration × AdrenalineThresholdFraction` (0.25 из [[UpgradesConfig]])
- `0 < remaining <= threshold` → `Mover.SetSpeedMultiplier(AdrenalineSpeedMultiplier)` (×1.4), иначе → 1
- `Timer.Finished` / `OnDisable` — сброс в 1

## Связи

- [[Timer]], [[Mover]], [[PlayerUpgrades]] (есть ли перк + параметры)

## Слабые места

- Дергает `SetSpeedMultiplier` **каждый кадр** с одинаковым значением (нет guard на смену) — безвредно, но шумно.
- Взаимодействие с будущими источниками множителей скорости (бафы/дебафы) не продумано: множитель один, аддитивной модели нет.
