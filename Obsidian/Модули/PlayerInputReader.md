# PlayerInputReader

**Слой:** Gameplay · **Код:** `Assets/MadSlime/Scripts/PlayerInput/` (PlayerInputReader, TouchJoystick, PlayerInputActions — автоген)

Обёртка New Input System для движения слайма.

## Механика

- `Awake` создаёт `PlayerInputActions` (карта Player, action Move: WASD + геймпад-стик); `OnEnable` подписывается и включает карту; `OnDestroy` — Dispose
- `MoveInput: Vector2` — читается [[Player]] каждый кадр
- **Событие `MovementKeyPressed`** — фронт «из нуля в неноль»; это стартовый триггер [[GameplaySessionHandler]] (раунд заморожен до первого движения)
- **TouchJoystick** (`OnScreenControl`): виртуальный стик — тап ставит базу в точку касания, drag двигает хэндл (ClampMagnitude, range 120), значение уходит в `<Gamepad>/leftStick`; визуал скрыт до касания; кормит тот же action

## Связи

- [[Player]] (MoveInput), [[GameplaySessionHandler]] (MovementKeyPressed), [[UiAnimations | UiEnableScheduler]] (показ HUD при первом движении)

## Слабые места

- TouchJoystick работает «при касании» и в редакторе всегда — поведение в редакторе отличается от десктопного флоу.
- Управление целиком одним action `Move` — эскейв/жестов нет (для аркады достаточно).
