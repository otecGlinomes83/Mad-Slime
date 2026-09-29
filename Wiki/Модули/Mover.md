# Mover

**Слой:** Gameplay · **Код:** `Assets/MadSlime/Scripts/Movement/Mover.cs`, `Rotator.cs`

Движение игрока без Rigidbody: `SmoothDamp` скорости + кламп позиции по полу, поверх — циклический «ползок» с пульсацией тяги.

## Механика

- `Move(direction)`: **без bounds — исключение** (bounds ставит только [[LevelGenerator]]); мёртвая зона ввода (sqrMagnitude < 0.05²) → плавное гашение; иначе SmoothDamp к `dir × speed × crawlScale` и сдвиг позиции, затем фаза ползка крутится по пройденным метрам
- **Ползок (Crawl)**: `_crawlPhase` — дистанционная фаза цикла (путь / шаг, шаг = `CrawlStride + скорость × StridePerSpeed`), поэтому частота растёт со скоростью сама; сброс фазы в 0 по фронту «стоял → пошёл» — каждый старт начинается с рывка. `_crawlStrength` (SmoothDamp по `CrawlRampTime`) — плавное включение/затухание цикла, на остановке гасит и тягу, и амплитуду деформации. Тяга: множитель скорости из `CrawlThrustCurve(phase)`, глубина `CrawlThrustDepth` (0 = ровная скорость, анимация остаётся); среднее кривой ≈1 сохраняет среднюю скорость — баланс не едет. На выбеге (`DecayVelocity`) фаза доезжает до конца цикла
- `ClampToBounds`: XZ кламп по Bounds пола ± радиус капсулы; радиус переписывает [[LevelScaler]] при росте — зона клампа сужается с ростом слайма (связь скрытая). У стены `_currentVelocity` не обнуляется — фаза крутится, слайм «борется» со стеной
- API: `SetDefaultSpeed`, `SetSpeedMultiplier` (дергает [[AdrenalineBoost]]), `SetSmoothTime`, `SetBounds`; свойства `Velocity`/`CurrentSpeed` (читает SpeedSmoke), `CrawlPhase`/`CrawlStrength` (читает [[Эффекты игрока | MovementDeformer]]). Конфиг приходит через `[Inject] Construct(PlayerConfig)` — валидация кривых Crawl именно там, не в Awake: порядок Awake со скоупом DI не закреплён
- **Rotator**: доворот `RotateTowards(LookRotation(dir))`, скорость из PlayerConfig (в ассете 1260°/с)

## Связи

- [[Player]], [[PlayerConfig]], [[LevelGenerator]] (bounds), [[AdrenalineBoost]], [[LevelScaler]], [[Камера | CameraFollow]]

## Слабые места

- Кламп по `_playerCollider.radius` без учёта lossyScale: согласовано только потому, что LevelScaler сам переписывает radius — при чужом скейле рута кламп поедет (`Mover.cs:131-140`).
- Гонка «bounds не выставлены» защищена только throw'ом в Move — порядок Awake LevelGenerator vs первый Update Player не закреплён.
