# PlayerTier

**Слой:** Gameplay · **Код:** `Assets/MadSlime/Scripts/Player/PlayerTier.cs`, `TierResolver.cs`

Масса и тир игрока — центральный узел событий роста.

## Механика

- `Add(amount)` (>= 0): масса += amount → `MassChanged(prev, cur)` → `CurrentTier = TierResolver.GetUnlockedTier(mass)` → `TierChanged(prev, cur)`
- **TierChanged стреляет всегда**, даже без смены тира — все подписчики обязаны фильтровать (LevelScaler, TierUpSound, CameraImpulse — у каждого свой `current <= previous → return`)
- **TierResolver**: сортирует пороги [[PlayerConfig]] по RequiredMass; `GetUnlockedTier` — наивысший пройденный; `GetSpeedFor` (fallback 4 — магия), `GetScaleFor`, `GetCameraOffsetFor`, `GetTierProgress` (доля между соседними порогами — рисует [[HUD | GrowthBarView]])

Тиры: `ItemTier { Small, Medium, Large, Boss }` ([[TierTable]] задаёт массу предметов).

## Связи

- Источник массы: [[Player]] (каждый укус)
- Подписчики TierChanged: [[LevelScaler]] (рост + скорость), [[Камера | CameraFollow/CameraImpulse]] (отъезд/пуш/FOV), TierUpSound ([[Аудио-подсистема]]), [[HUD | GrowthBarView]]; радиусы детекторов тоже масштабируются тиром ([[Детекция и сбор]])

## Слабые места

- Событие «TierChanged» без смены тира на каждый съеденный предмет — 5+ подписчиков делают лишнюю работу; контракт «фильтруй сам» нигде не написан.
- Баланс порогов (PlayerConfig) и масс предметов ([[TierTable]]) живут в двух таблицах — см. [[PlayerConfig]].
