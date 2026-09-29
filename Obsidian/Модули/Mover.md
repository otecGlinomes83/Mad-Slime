# Mover

**Слой:** Gameplay · **Код:** `Assets/MadSlime/Scripts/Movement/Mover.cs`, `Rotator.cs`

Движение игрока без Rigidbody: `SmoothDamp` скорости + кламп позиции по полу.

## Механика

- `Move(direction)`: **без bounds — исключение** (bounds ставит только [[LevelGenerator]]); мёртвая зона ввода (sqrMagnitude < 0.05²) → плавное гашение; иначе SmoothDamp к `dir × speed` и сдвиг позиции
- `ClampToBounds`: XZ кламп по Bounds пола ± радиус капсулы; радиус переписывает [[LevelScaler]] при росте — зона клампа сужается с ростом слайма (связь скрытая)
- API: `SetDefaultSpeed`, `SetSpeedMultiplier` (дергает [[AdrenalineBoost]]), `SetSmoothTime`, `SetBounds`; свойства `Velocity`/`CurrentSpeed` (читают [[Эффекты игрока | MovementDeformer]], SpeedSmoke)
- **Rotator**: доворот `RotateTowards(LookRotation(dir))`, скорость из PlayerConfig (420°/с)

## Связи

- [[Player]], [[PlayerConfig]], [[LevelGenerator]] (bounds), [[AdrenalineBoost]], [[LevelScaler]], [[Камера | CameraFollow]]

## Слабые места

- Кламп по `_playerCollider.radius` без учёта lossyScale: согласовано только потому, что LevelScaler сам переписывает radius — при чужом скейле рута кламп поедет (`Mover.cs:131-140`).
- Гонка «bounds не выставлены» защищена только throw'ом в Move — порядок Awake LevelGenerator vs первый Update Player не закреплён.
