# Player

**Слой:** Gameplay · **Код:** `Assets/MadSlime/Scripts/Player/Player.cs`

Главный контроллер слайма: ввод → движение/поворот, и — ядро прогресса — обработка каждого съеденного предмета.

## Update

`MoveInput` → мировое направление (Y=0) → `Mover.Move(dir)` + `Rotator.Rotate(dir)`. Скорость: `BaseMoveSpeed × SpeedMultiplier` (апгрейд Speed), дальше [[LevelScaler]] перекрывает скоростью тира × тот же множитель.

## OnItemCollected(item) — прогресс за один укус

1. Тир предмета == тир игрока → `ScalePunch.Punch()` (squash)
2. `isQuota = LevelProgress.IsQuotaItem(definition)` — предмет ещё нужен по квоте
3. `mass = max(1, round(TierMass × (isQuota ? QuotaMassMultiplier : MassMultiplier)))` — множители из [[PlayerUpgrades]] (Quota = Appetite×Taste, обычная = Appetite)
4. `fillWeight = isQuota ? 1 : ForeignFillMultiplier` (Метаболизм ускоряет «заливку» посторонними)
5. `LevelProgress.RegisterCollected(definition, fillWeight)` → `PlayerTier.Add(mass)`

Компоненты по RequireComponent: [[Mover]], Rotator, [[PlayerTier]].

## Связи

- [[Детекция и сбор | Collector]] (источник событий), [[LevelProgress]], [[PlayerTier]] → [[LevelScaler]], [[Камера]], [[PlayerConfig]], [[PlayerUpgrades]], [[PlayerInputReader]]

## Слабые места

- `isQuota` вычисляется **до** `RegisterCollected`: предмет, закрывающий квоту этим вызовом, получает квотный множитель массы, а следующий такой же — уже нет (`Player.cs:115-121`) — семантическая рассинхронизация массы и заливки.
- Порядок вызова подписчиков `Collector.ItemCollected` (Player / пул / звук) не детерминирован — задаётся порядком OnEnable.
