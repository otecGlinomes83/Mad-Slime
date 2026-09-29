# LevelScaler

**Слой:** Gameplay · **Код:** `Assets/MadSlime/Scripts/Player/LevelScaler.cs`

Плавный рост слайма при повышении тира — модель, коллайдер, скорость.

## Механика

- `OnEnable`: синхронизация с текущим тиром + **перекрытие скорости**: `Mover.SetDefaultSpeed(GetSpeedFor(tier) × SpeedMultiplier)` — то, что выставил [[Player]] из BaseMoveSpeed, живёт только до первого тир-апа
- `OnTierChanged` (только при реальной смене): цель масштаба = `GetScaleFor(tier)`, новая скорость, `GrowAsync` — свой CTS (linked с destroy-token), предыдущий отменяется
- `GrowAsync`: LerpUnclamped с back-out easing (овершут ×1.7, 0.55 с); масштабирует модель, коллайдер (height/radius/center.y, только при |Δ| ≥ 0.01) и держит рут на «рте» (`position.y = collider.radius`)

## Связи

- [[PlayerTier]], [[PlayerConfig | пороги тиров]], [[Mover]] (скорость и радиус клампа), [[PlayerUpgrades]] (SpeedMultiplier), [[Камера | CameraFollow]] (отъезд по тому же тиру)

## Слабые места

- Аккуратный CTS — единственный в проекте место с ручным dispose; хрупко при копипасте.
- Скрытый контракт с Mover: кто владеет скоростью — Player или LevelScaler — определяется порядком инициализации, а не явно.
