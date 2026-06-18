# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working in this repository.

---

## ⚠️ ПЕРЕД КАЖДЫМ ОТВЕТОМ — ПЕРЕЧИТАЙ ОБЯЗАТЕЛЬНО

Перед **каждым** сообщением пользователя (каждым своим ответом) ты **обязан** перечитать:

1. **`AI_RULES.md`** — канон стиля, нейминг, форматирование, запреты, UniTask-паттерны, чек-лист перед отдачей кода.
2. **`AI_CONTEXT.md`** — текущее состояние проекта: что сделано в сессии, что готово, что не готово, тех. долг, GDD.

Это не опционально. Свежий контекст сессии — в `AI_CONTEXT.md`. Глобальные правила — в `AI_RULES.md`. Без перечитывания ты работаешь вслепую.

Если файла нет или он пустой — скажи об этом явно, не выдумывай правила.

---

## Companion files (источники правды)

- **`AI_RULES.md`** — стиль кода, нейминг, форматирование, запреты (`var`, комментарии, лямбды в `+=`, `!`, и т.д.), UniTask-паттерны, чек-лист перед отдачей кода.
- **`AI_CONTEXT.md`** — состояние проекта, прогресс сессии, что сделано / не сделано, тех. долг, GDD.

Не дублируй их содержимое в этом файле — ссылайся и перечитывай.

---

## Project

- **Engine:** Unity **2022.3.62f2 LTS**, 3D (low-poly arcade).
- **Target:** mobile-first. Избегать GC-аллокаций, NonAlloc API, никакого LINQ в Update.
- **IDE:** JetBrains Rider. `Assets/Editor/SetRiderAsDefaultEditor.cs` автоматически прописывает Rider как Script Editor и вычищает пакеты VS/VSCode.
- **Стек:** UniTask (Cysharp) для async, New Input System, TextMeshPro. Нет DI / ECS / event bus / singleton-ов.

## Build / run

- Нет CLI-скриптов сборки — Unity Editor делает build (File → Build Settings, Android/iOS).
- Открывать проект строго через Unity 2022.3.62f2 (см. `ProjectSettings/ProjectVersion.txt`).
- Тестовой инфраструктуры нет: ни `*.asmdef` с тестами, ни EditMode/PlayMode папок. `Test Framework` пакет в кэше, но не настроен.
- `.csproj` / `.sln` генерятся Unity — не редактировать руками. `Mad-Slime.sln` лежит в корне (см. git tracking).

---

## Архитектура (big picture)

### Принципы
- **SRP жёсткий.** Один класс — одна ответственность — один файл. `Mover` не знает про `Rotator`, `Health` не знает про `Healer`, `Chaser` не знает про `Attacker`.
- **Композиция через `[RequireComponent]` + `GetComponent` в `Awake`.** Не наследование, не сервис-локатор, не singleton.
- **Зависимости явно.** Дизайнерские значения: `[SerializeField] private`. Рантайм-зависимости: либо `[RequireComponent]`, либо Setup-метод.
- **События — только `event Action<T>`.** Никаких `UnityEvent`. `OnEnable` / `OnDisable` пара, никаких анонимных лямбд в `+=`.
- **Источник истины в одном месте.** UI читает через события, не дёргает состояние компонентов напрямую.

### Ключевые абстракции

| Абстракция | Файл | Роль |
|---|---|---|
| `Health` | `Assets/Scripts/Health/Health.cs` | HP + i-frames. События: `Damaged`, `Died`, `ValueChanged`, `InvulnerabilityEnded`. |
| `Healer` | `Assets/Scripts/Health/Healer.cs` | Реген **после** окончания i-frames. Подписан на `InvulnerabilityEnded`, не на `Damaged`. |
| `Timer` | `Assets/Scripts/Game/Timer.cs` | Универсальный таймер. `Setup(duration)` → `StartCount()` / `Stop()` / `Continue()`. События: `Ticked(float)`, `Finished`. UniTask-цикл с `GetCancellationTokenOnDestroy()`. |
| `ITarget` | `Assets/Scripts/Interfaces/ITarget.cs` | Маркер цели: `{ Transform, Health }`. Реализует `Player`. |
| `TargetSensor` | `Assets/Scripts/Detectors/TargetSensor.cs` | OverlapSphereNonAlloc по `LayerMask` → ищет `ITarget` → события `TargetEntered` / `TargetExited`. |
| `GenericOverlapDetector<T>` | `Assets/Scripts/Detectors/GenericOverlapDetector.cs` | Базовый детектор по `T`, кэшированный `Collider[32]`. Наследники: `ItemDetector`, `AttractableDetector`. |
| `Mover` | `Assets/Scripts/Movement/Mover.cs` | Движение через `CharacterController.SimpleMove` + `Vector3.SmoothDamp`. Принимает 3D-вектор произвольной длины, нормализует сам. |
| `Rotator` | `Assets/Scripts/Movement/Rotator.cs` | Поворот **только по Y** (обнуляет `direction.y` перед `LookRotation`). Mover получает полный 3D-вектор как раньше. |
| `Player` | `Assets/Scripts/Player/Player.cs` | Оркестратор: `[RequireComponent]` на Mover/Rotator/PlayerMass/Health/Healer. Читает input, конвертит в world-space через камеру. |

### Цепочки (порядок исполнения)

**Сбор предмета:**
`ItemDetector` (GenericOverlap) → `Collector.ItemCollected` → `Player.OnItemCollected` → `Inventory.Add` + `PlayerMass.Add` → `PlayerScaler` визуально растёт.

**Получение урона + реген:**
`Attacker` (враг, `TargetSensor` + `Timer` cooldown) → `target.Health.TakeDamage(1)` → `i-frames 0.5s` → `Timer.Finished` → `Health.InvulnerabilityEnded` → `Healer.StartRegen` (5s) → `Health.Heal(toMax)`.

**Движение игрока:**
`PlayerInputReader.MoveInput (Vector2)` → `Player.ConvertToWorldDirection` (через камеру) → `_mover.Move(direction)` + `_rotator.Rotate(direction)`.

**Поведение врага:**
`Enemy` (Wander) → при `TargetEntered` → `Chaser.Tick(playerPos)` → при дистанции атаки → `Attacker.TryAttack` (cooldown через `Timer`).

**Добыча (Prey):** симметрично врагу, но `Fleer` (убегает по `-direction`).

### Структура `Assets/Scripts/`

```
Camera/         — CameraFollow
Collectables/   — Item, ItemDefinition (SO)
Collector/      — Collector, CollectableAttractor, ItemDetector
Combat/         — Attacker.cs (⚠ namespace: NPC.Enemy, см. тех. долг)
Detectors/      — GenericOverlapDetector, TargetSensor, AttractableDetector
Game/           — SessionHandler, Timer
Health/         — Health, Healer
Interfaces/     — ITarget, IAttractable, IMassHolder, ISpawner, IState (unused)
Inventory/      — Inventory
Movement/       — Mover, Rotator
NPC/Enemy/      — Enemy, Chaser, Attacker
NPC/Prey/       — Prey, Fleer
Player/         — Player, PlayerMass, PlayerScaler, PlayerInputReader
PlayerInput/    — inputActions, PlayerInputActions, PlayerInputReader
Quota/          — QuotaEntry, QuotaTreker, PropsBank
Spawners/       — GenericSpawner, EnemySpawner, ItemSpawner
UI/             — HealthUI, TimerUI
```

### Editor

- `Assets/Editor/SetRiderAsDefaultEditor.cs` — `[InitializeOnLoad]`, ставит Rider как default script editor на Windows (реестр + `EditorPrefs`), удаляет пакеты VS/VSCode. Жёстко прописан путь `G:\1. CodeETC\Rider` первым приоритетом.

---

## Известные разрывы (НЕ реализовано, подробности в `AI_CONTEXT.md`)

Эти места — источник следующих задач. Если доделываешь, начни с них.

- **Квота не подключена.** `QuotaTreker` есть, `ItemDefinition` (SO) есть, но `Collector.ItemCollected` не дёргает квоту.
- **Нет Game Over / смерти.** `Health.Died` ни на что не подписан, экран проигрыша отсутствует.
- **Враги бессмертны.** Нет урона по врагам, нет `DeathZone`.
- **Нет префабов и ассетов.** Пустые `Prefabs/`, `Materials/`, `Meshes/`. ScriptableObject-ассеты `ItemDefinition` отсутствуют. Только `Test.unity` сцена.
- **UI минимален.** Только `HealthUI` + `TimerUI`. Нет Win/Lose, паузы, меню, HUD квоты/уровня, мобильного джойстика.
- **Метапрогрессия отсутствует** целиком: валюта, навыки (3 ветки по GDD), ребитх, сохранения, скины.

---

## Тех. долг (см. `AI_CONTEXT.md`)

- `IState` определён, но не используется (заготовка под FSM).
- `Wander` / `Chaser` / `Fleer` в разных неймспейсах: `Chaser` в глобальном, `Wander` / `Fleer` в `NPC.Prey`. Унифицировать.
- Два `Attacker.cs`: один в `Combat/` (глобальный namespace, не подключён), второй в `NPC/Enemy/` (используется). Удалить лишний.
- `PlayerMass.Setup()` дёргает `Changed?.Invoke(_defaultMass, _mass)` — странный контракт для подписчиков.
- `Inventory` подключён к `Player`, но без UI.
- Hard-coded путь `G:\1. CodeETC\Rider` в `SetRiderAsDefaultEditor.cs` — убрать или вынести в настройки.

---

## Чего не делать

- **Не создавай `Manager` / `Handler` / `Utility` / `Helper`** без явной причины — см. `AI_RULES.md`.
- **Не дёргай абстракции раньше времени.** YAGNI: нет квоты — нет `IQuotaService`, нет метапрогрессии — нет `IMetaProgressionProvider`.
- **Не смешивай NPC-неймспейсы.** Новый NPC — клади в `NPC/<Role>/` и используй namespace, как у соседей.
- **Не используй `static` состояние** (кроме Editor-инициализаторов).
- **Не подписывайся анонимной лямбдой** на событие — только именованный метод + `OnEnable` / `OnDisable` пара.
- **Не выдумывай Unity API** — ищи в актуальной доке или `Library/PackageCache/`. Несуществующих оверрайдов / методов не бывает.
- **Не лезь в `Library/`, `obj/`, `UserSettings/`, `Logs/`** — это не наш код, и оно в `.gitignore`.
