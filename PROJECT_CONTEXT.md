# Mad Slime — Контекст проекта

Документ для нейросети, которая подхватывает проект. Описывает: что за игра, что уже сделано, что планируется, какая архитектура.

Связан с `AI_RULES.md` — там правила написания кода.

---

## 1. Концепция игры

**Жанр:** 3D action / поглотитель (вдохновлено Katamari Damacy / Tasty Blue / agar.io).

**Игрок:** слайм, который катается по сцене и поглощает объекты, **наращивая массу**. Чем больше масса — тем больше и тяжелее становится сам слайм.

**Что можно поглощать:**

- Простые предметы (монеты, мелочь, пикапы)
- NPC (люди, гуманоиды) — тоже коллектабл, со своим AI
- Физические объекты (бочки, ящики и т.п.)

**Все игровые объекты (игрок, NPC, коллектаблы) — на Rigidbody.** Никаких CharacterController. Решение принято для единообразия движения и взаимодействия с зоной притяжения.

**Ключевая механика:** игрок имеет **две зоны** вокруг себя:

- **Внутренняя (Detection)** — коллектабл сразу хватается и анимированно поглощается
- **Внешняя (Attraction)** — коллектабл начинает «засасываться» в сторону игрока, пока не попадёт во внутреннюю зону

Это даёт ощущение силы и засасывания, как пылесос.

---

## 2. Что реализовано

### 2.1 Игрок

**`Player.cs`** — оркестратор:

- `[RequireComponent(typeof(Rigidbody))]`, `Mover`, `Rotator`
- Зависимости: `_inputReader`, `_cameraTransform`, `_collector` через `[SerializeField]`
- В `Update` читает input и считает `_moveDirection` относительно камеры
- В `FixedUpdate` применяет `Mover.Move` и `Rotator.Rotate` с закэшированным направлением
- Подписан на `_collector.OnCollect`, увеличивает массу

**`Mover.cs`** — движение игрока через `Rigidbody.velocity`. Сохраняет Y velocity (гравитация работает), управляет X/Z через `Vector3.MoveTowards` с настраиваемыми acceleration/deceleration. Вызывается из `Player.FixedUpdate`.

**`Rotator.cs`** — поворот через `Rigidbody.MoveRotation` (`Quaternion.RotateTowards` с `_rotationSpeed`). Вызывается из `Player.FixedUpdate`.

**`PlayerInputReader`** — обёртка над Unity Input System. Подписывается на `Move.performed`/`canceled`, экспонирует `Vector2 MoveInput`. Лежит в `Assets/Scripts/PlayerInput/`.

### 2.2 Сбор коллектаблов

Чистое разделение через SRP:

**`CollectableDetector.cs`** (на игроке)

- Каждый кадр делает `Physics.OverlapSphereNonAlloc` с кэшированным буфером
- На каждый найденный коллектабл шлёт событие `Detected(ICollectable, Transform)`
- Рисует gizmo радиуса детекта
- **Не мутирует** ничего — чистая детекция

**`Collector.cs`** (на игроке)

- `[SerializeField] private CollectableDetector _detector`
- Подписывается на `_detector.Detected` в `OnEnable`, отписывается в `OnDisable`
- В обработчике:
    1. Зовёт `collectable.Collect()` — коллектабл сам останавливает свои процессы (отключает коллайдер, чтобы детектор больше не нашёл)
    2. Запускает `AbsorbAsync` через UniTask
- Анимация: `SmoothStep` по позиции и скейлу к игроку, `await UniTask.Yield(PlayerLoopTiming.Update)`
- Cancellation через `this.GetCancellationTokenOnDestroy()`
- После анимации:
    1. Зовёт `collectable.Release()` (коллектабл шлёт `ReadyToRelease`)
    2. Шлёт `OnCollect(mass)` — игрок получает массу

### 2.3 Контракт коллектабла

**`ICollectable.cs`**

```csharp
public interface ICollectable
{
    int Mass { get; }

    event Action<ICollectable> ReadyToRelease;

    void Collect();   // старт сбора — остановить процессы, визуал жив
    void Release();   // конец анимации — шлёт ReadyToRelease для пула
}
```

**`SimpleCollectable.cs`** — реализация для простых предметов:

- `[RequireComponent(typeof(Collider))]`
- В `Collect()` отключает коллайдер
- В `Release()` шлёт `ReadyToRelease` (gameObject отключает пул, не сам коллектабл)

---

## 3. Что в работе

### 3.1 `SlimeConfig` (ScriptableObject, в Configs/)

Файл создан, конкретное содержимое пока не оформлено. Предполагаемые параметры (на согласовании):

- Базовая масса
- Скорость движения от массы
- Радиус детекции от массы
- Радиус притяжения от массы
- Acceleration / Deceleration
- Прочие настройки слайма

---

## 4. Что планируется

### 4.1 Зона притяжения (Attractor)

**Не реализовано. Архитектура обсуждена, выбор подхода пока не сделан.**

Зона больше, чем зона детекта. Внутри неё коллектаблы летят/едут к игроку. Когда доезжают до зоны детекта — попадают в `Collector`.

**Варианты движения** (всё на Rigidbody):

| Тип | Setup | Движение в `Attract` |
|---|---|---|
| Простой (монета) | Non-kinematic Rigidbody + Collider, во время сбора `isKinematic=true` | `_rigidbody.MovePosition(...)` или velocity |
| NPC-человек | Non-kinematic Rigidbody + Collider, freezeRotation X/Z | `_rigidbody.velocity = direction * pullSpeed` (с сохранением Y) |
| Физический (бочка) | Non-kinematic Rigidbody | `AddForce(direction * force, ForceMode.Force)` |

**Открытый вопрос:** как структурировать движение в коде. Варианты:

- **A:** добавить `Attract(Vector3, float)` в `ICollectable` (минус — нарушение SRP)
- **B:** отдельный `IAttractable` интерфейс (плюс — SRP)
- **C:** компонент-стратегия `AttractableMover` (плюс — чистая композиция)

**Ждём решения от меня.**

### 4.2 Пул коллектаблов

**Не реализовано. Подготовлено в архитектуре.**

`SimpleCollectable.Release()` шлёт событие `ReadyToRelease(ICollectable)`. Будущий пул:

- Подпишется на это событие при выдаче объекта
- Заберёт объект обратно при срабатывании
- Сбросит позицию/скейл, выдаст следующему запросившему

### 4.3 NPC-коллектаблы

**Не реализовано.**

NPC будет:
- На `Rigidbody` (non-kinematic, freezeRotation X/Z чтоб не падал)
- Со своим AI / FSM (бродит, реагирует на окружение)
- Реализует `ICollectable` (Mass, Collect, Release)
- При `Collect()` отключает AI/процессы, передаёт управление аттрактору
- При `Release()` пул его реклеймит

**Открытый вопрос:** как взаимодействуют AI и притяжение:

- Перехват управления (AI выключается на время притяжения)
- Сложение сил (AI + притяжение работают параллельно)
- FSM-состояние "ATTRACTED" в самом NPC

### 4.4 Прочее, что обсуждалось мельком

- Рост слайма от массы (визуальный + физический скейл)
- Возможно — разные виды слайма / конфиги через `SlimeConfig`

---

## 5. Структура проекта

```
Assets/Scripts/
├── Player/
│   ├── Player.cs                    # оркестратор игрока, Setup, держит компоненты
│   ├── Collector.cs                 # реакция на детект, анимация поглощения (UniTask)
│   ├── CollectableDetector.cs       # OverlapSphere детект, событие Detected
│   └── Rotator.cs                   # поворот
├── Movement/
│   └── Mover.cs                     # движение CharacterController
├── Collectables/
│   └── SimpleCollectable.cs         # ICollectable для простых предметов
├── Interfaces/
│   └── ICollectable.cs              # контракт коллектабла
└── Configs/
    └── SlimeConfig.cs               # ScriptableObject (WIP)
```

---

## 6. Архитектурные решения

### 6.1 SRP в действии

- **Detector** только детектит, ничего не мутирует
- **Collector** только реагирует на детект и крутит анимацию
- **Collectable** только пассивные данные + ответы на `Collect`/`Release`

### 6.2 Pull vs Push модель событий

Используется **push-модель через C# events (`Action<T>`)**:

- Детектор пушит данные в Collector
- Коллектабл пушит `ReadyToRelease` в пул

**Никаких UnityEvent.** Никаких анонимных лямбд при подписке.

### 6.3 Async

- **UniTask** — основной инструмент async
- Все async-операции с `CancellationToken` через `this.GetCancellationTokenOnDestroy()`
- `OperationCanceledException` ловится явно
- Fire-and-forget через `.Forget()`

### 6.4 Pooling-friendly с самого начала

Коллектабл не уничтожается и не отключается сам (после исправления). Он только шлёт `ReadyToRelease`. Пул будет владельцем lifecycle.

### 6.5 Композиция > наследование

Player не наследник чего-то — он держит компоненты:
`_mover`, `_rotator`, `_inputHandler`, `_collector`.

---

## 7. Зависимости / ассеты

- **Unity** (версия — проверить в `ProjectSettings/ProjectVersion.txt`)
- **UniTask** (Cysharp.Threading.Tasks) — установлен, активно используется
- **TextMeshPro** — пока не задействован, но в правилах закреплён как обязательный для UI

---

## 8. Открытые вопросы (что ждёт решения от меня)

1. Какой подход к движению притягиваемых коллектаблов (A / B / C из раздела 4.1)
2. Скорость притяжения — постоянная, от расстояния или от массы коллектабла
3. Как ведёт себя AI NPC при попадании в зону притяжения
4. Содержимое `SlimeConfig`
5. Когда делать пул — сейчас или после прототипа всего цикла

---

## 9. Что НЕ обсуждалось / не определено

- UI (HUD, меню)
- Прогрессия / уровни / счёт
- Звук
- Камера (CinemaChine?)
- Build pipeline / платформы
- Сохранение
- Враги, которые атакуют слайма
- Условия победы / поражения

Эти темы поднимать только когда я о них спрошу.

---

## 10. Flow поглощения (sequence)

```
[Frame N]
  CollectableDetector.Update()
    └── Physics.OverlapSphereNonAlloc(...) -> [collider]
         для каждого hitCollider:
           TryGetComponent<ICollectable>()
           └── fire Detected(collectable, hitCollider.transform)
                └── Collector.OnCollectableDetected(...)
                     ├── collectable.Collect()
                     │     └── SimpleCollectable: _collider.enabled = false
                     └── AbsorbAsync(collectable, transform).Forget()
                            (UniTaskVoid запущен, Update продолжается)

[Frame N+1 ... N+K] (пока идёт анимация)
  Collector.AnimateAbsorptionAsync (UniTask loop)
    └── transform.position = Lerp(start, player.position, smoothedProgress)
    └── transform.localScale = Lerp(startScale, zero, smoothedProgress)
    └── await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken)

  CollectableDetector.Update()
    └── OverlapSphere НЕ находит коллектабл (коллайдер выключен) ✓

[Frame N+K, анимация закончилась]
  Collector.AbsorbAsync продолжается:
    ├── int mass = collectable.Mass
    ├── collectable.Release()
    │     └── SimpleCollectable: ReadyToRelease?.Invoke(this)
    │           └── (будущий) Pool.Reclaim(collectable)
    └── OnCollect?.Invoke(mass)
          └── Player.IncreaseMass(mass)

[При уничтожении игрока во время анимации]
  GetCancellationTokenOnDestroy() триггерит cancellation
  └── ThrowIfCancellationRequested() кидает OperationCanceledException
       └── catch блок -> return (без Release, без OnCollect)
```

---

## 11. Setup в Unity (как настроить сцену)

### Игрок (GameObject)

Компоненты на корневом GameObject игрока:

- `Rigidbody` (non-kinematic) — **`Constraints: Freeze Rotation X, Z`** (чтоб не падал на коллизиях; Y вращает `Rotator` через `MoveRotation`)
- `Collider` (Sphere/Capsule)
- `Player`
- `Mover` — `_maxSpeed`, `_acceleration`, `_deceleration`
- `Rotator` — `_rotationSpeed`
- `PlayerInputReader`
- `CollectableDetector` — `_detectionRadius`, `_collectableMask`
- `Collector` — в инспекторе перетянуть `_detector` (компонент с того же GO), задать `_absorptionDuration`

В `Player` инспекторе:
- `_inputReader` — компонент `PlayerInputReader` с того же GO
- `_cameraTransform` — Transform главной камеры (для конвертации input в мировое направление)
- `_collector` — компонент `Collector` с того же GO

### Коллектабл (Prefab)

Простой коллектабл (`SimpleCollectable`):

- `Collider` на нужном лейере (тот же, что в `CollectableDetector._collectableMask`)
- `Rigidbody` — non-kinematic по умолчанию (чтобы падал/реагировал на физику до сбора)
- `SimpleCollectable` — `_mass` в инспекторе
- Визуал (Mesh / Sprite)
- Слой коллайдера должен совпадать с маской детектора

**Во время поглощения** `SimpleCollectable.Collect()` ставит `_rigidbody.isKinematic = true` — это отключает физическую интеграцию, чтобы анимация Collector'а через `transform.position` и `transform.localScale` не конфликтовала с физикой.

**Когда появится пул** — коллектабл регистрируется в пуле, который слушает его `ReadyToRelease`.

---

## 12. Как добавить новый тип коллектабла

### Шаги

1. Создать новый класс в `Assets/Scripts/Collectables/`:

```csharp
using System;
using UnityEngine;

namespace Collectables
{
    [RequireComponent(typeof(Collider))]
    [RequireComponent(typeof(Rigidbody))]
    public sealed class HumanCollectable : MonoBehaviour, ICollectable
    {
        [SerializeField] private int _mass = 10;

        private Collider _collider;
        private Rigidbody _rigidbody;
        private HumanAI _ai;

        public int Mass => _mass;

        public event Action<ICollectable> ReadyToRelease;

        private void Awake()
        {
            _collider = GetComponent<Collider>();
            _rigidbody = GetComponent<Rigidbody>();
            _ai = GetComponent<HumanAI>();
        }

        public void Collect()
        {
            _collider.enabled = false;
            _rigidbody.isKinematic = true;
            _ai.enabled = false;
        }

        public void Release()
        {
            ReadyToRelease?.Invoke(this);
        }
    }
}
```

2. Положить на префаб человека с `Rigidbody` (freezeRotation X/Z) и AI-компонентом.

3. На префабе выставить слой, соответствующий `_collectableMask` детектора.

**Никаких изменений** в `CollectableDetector` или `Collector` не нужно — они работают через `ICollectable`.

---

## 13. FAQ для AI, подхватывающего проект

### Q: Почему в `Collector` нет HashSet для дедупликации?

Потому что `Collect()` на коллектабле **отключает коллайдер**. Следующий вызов `OverlapSphere` его уже не найдёт. HashSet был бы дублированием логики.

### Q: Почему `Collect` и `Release` — отдельные методы?

`Collect` — старт сбора (остановка процессов, визуал жив, идёт анимация поглощения).
`Release` — конец сбора (анимация завершилась, объект готов уйти в пул).

Между ними проходит время анимации.

### Q: Почему детектор не делает поглощение сам?

**SRP.** Детектор только детектит. Поглощением занимается `Collector`. Они общаются через событие `Detected`.

### Q: Почему `AbsorbAsync` возвращает `UniTaskVoid`, а не `UniTask`?

Это fire-and-forget — никто не ждёт результата. `UniTaskVoid` оптимизирован для таких сценариев (меньше overhead, чем `UniTask` без await).

### Q: Где обработка исключений в `AnimateAbsorptionAsync`?

В `AbsorbAsync` обёрнуто в `try/catch (OperationCanceledException)`. Сама анимация просто бросает исключение через `cancellationToken.ThrowIfCancellationRequested()` или `await ... cancellationToken`.

### Q: Куда добавлять зону притяжения?

Скорее всего — отдельный компонент `CollectableAttractor` на игроке, **параллельно** с `CollectableDetector`. Не смешивать.

### Q: Почему всё на Rigidbody, а не CharacterController?

Потому что коллектаблы и NPC должны взаимодействовать с зоной притяжения единым способом. Смесь CC и Rigidbody плохо стыкуется (CC не пушится силами, Rigidbody не имеет встроенной поддержки степов и слоупов CC). Единый стек на Rigidbody проще и предсказуемее.

### Q: Почему `SimpleCollectable.Collect()` переключает в `isKinematic`?

Анимация поглощения в `Collector` напрямую меняет `transform.position` и `transform.localScale`. На non-kinematic Rigidbody это конфликтует с физическим интегратором (объект может прыгать или дёргаться). `isKinematic = true` отключает физическую интеграцию, и transform-движение работает чисто.

### Q: Можно ли использовать корутины вместо UniTask?

Нет. UniTask — стандарт проекта. Корутины — только если задача *супер*-простая (например, `WaitForSeconds` и одно действие).

### Q: Можно ли добавить новый интерфейс / абстрактный класс?

Только если есть **реальная** причина (полиморфизм, разные реализации). Не "на будущее". См. правило YAGNI в AI_RULES.md.

### Q: Что насчёт ECS / DOTS?

Не используется. Классический MonoBehaviour-based стек. Не предлагать.

---

## 14. Точки расширения (на будущее)

Архитектура подготовлена к этим расширениям без рефакторинга:

| Что | Где зацепиться |
|---|---|
| Новый тип коллектабла | Реализовать `ICollectable` (см. раздел 12) |
| Пул объектов | Подписаться на `ICollectable.ReadyToRelease` |
| Зона притяжения | Новый компонент на игроке, рядом с `CollectableDetector` |
| Звук при сборе | Подписаться на `Collector.OnCollect` |
| UI счётчика массы | Подписаться на изменение массы в `Player` |
| VFX при поглощении | Подписаться на `Collector.OnCollect` или на `ICollectable.ReadyToRelease` |
