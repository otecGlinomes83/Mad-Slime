# Правила для AI: Unity C# Gameplay Programming

Это инструкции для нейросети, помогающей мне писать код. Следовать строго.

---

## Кто я

- Senior Unity gameplay programmer
- Пишу production-ready C# код для геймплейных систем
- Знаю что делаю — объяснения и расшаркивания не нужны
- Общаюсь на русском, код — на английском
- Прямой стиль, без расшаркиваний

## Стиль общения

- **Терсе, без воды.** Никаких "great idea!", "you're right!", "let me help you".
- **Не объяснять код**, если не попросил явно. Сделал — показал — всё.
- **Не предлагать улучшения** "на будущее" или "ещё можно сделать". Только то, что просил.
- **Если задал уточняющий вопрос и я его скипнул — НЕ предполагай ответ, жди явного ответа от меня.**
- **Не создавать левых файлов**: README, документация, прогресс-логи — только если попросил.
- Прямой тон, без излишней вежливости.

## Перед тем как кодить

1. Если задача нетривиальная — короткий план, затем по шагам
2. Если что-то непонятно — задай конкретный вопрос с вариантами выбора
3. Если знание Unity API неточное — ищи в актуальной доке, не выдумывай
4. Не лезь в файлы, которые не относятся к задаче

---

## C# Базовые правила

### Запрещено

- `var` — всегда явные типы
- LINQ если создаёт аллокации или ухудшает читаемость
- Reflection
- `UnityEvent` — только C# `Action<T>` события
- Анонимные lambda при подписке на события (`+= () => ...`)
- `static` классы/состояние, если не критично
- `GameObject.Find`, `Transform.Find`, `FindObjectOfType`
- Service Locator паттерн
- Singleton — только если явно попросил
- Создавать классы с именами `Manager`, `Handler`, `Utility`, `Helper` (если ответственность не очевидна)
- Создавать интерфейсы "на будущее"
- Глубокие иерархии наследования
- Магические абстракции
- Hidden side effects
- Комментарии в коде. Никогда.
- Тернарный оператор (`condition ? a : b`) — писать обычным `if/else`, явнее.

### Обязательно

- Композиция > наследование
- 1 класс = 1 ответственность
- Короткие сфокусированные методы
- Early returns вместо вложенных if
- Поток выполнения сверху вниз — код читается линейно
- YAGNI, KISS, DRY, SOLID — прагматично, без догматизма
- Явные зависимости через Setup-метод, конструктор или SerializeField
- Простые FSM где есть состояния

---

## Naming Conventions

| Что | Стиль | Пример |
|---|---|---|
| Private field | `_fieldName` | `_mover`, `_collider` |
| Private static | `s_fieldName` | `s_instance` |
| Constants | `PascalCase` | `BufferSize`, `MaxHealth` |
| Methods | `PascalCase` | `Move`, `OnEnable` |
| Events | `PascalCase` | `Collected`, `Damaged` |
| Local vars | `camelCase` | `elapsedTime`, `hitsCount` |
| Arguments | `camelCase` | `targetPosition`, `pullSpeed` |
| Interfaces | `IInterfaceName` | `ICollectable`, `IAttractable` |
| Bool fields/properties | `_isSomething` / `IsSomething` | `_isCollected`, `_isSetupFinished` |

### Имена переменных

- **Никаких однобуквенных имён.** Не `t`, не `smooth`, не `c`.
- Исключение: `i`, `j` как счётчики простых for-циклов.
- Имена должны быть описательными: `progress`, `smoothedProgress`, `elapsedTime`, `hitCollider`, `cancellationToken`.

---

## Форматирование

- **Открывающая фигурная скобка всегда на новой строке.**
- Всегда braces для if/else/loops (даже однострочных).
- Логические блоки разделяются пустыми строками.
- Удалять unused using-директивы.
- **Один файл = один класс.**
- Сравнение с false через `== false`, не `!`:

```csharp
if (collectable.IsActive == false)
{
    return;
}
```

- Тип Collider, не Collider2D по умолчанию (3D игра).

---

## Архитектура

- **SRP — святое.** Если класс делает две вещи — разделяй.
- Маленькие геймплейные компоненты лучше одного "толстого".
- Явные зависимости (SerializeField для редактора, Setup для рантайма).
- Прозрачный execution flow > "умная" архитектура.
- **Не создавать абстракции до того, как они реально нужны.**
- Интерфейсы — только если есть реальная причина (полиморфизм, разные реализации).
- Пассивные объекты (data) отдельно от активных систем (behavior).

### Пример правильного разделения

- `CollectableDetector` — только детект через OverlapSphere, шлёт событие
- `Collector` — реагирует на событие, делает анимацию и сбор
- `SimpleCollectable` — пассивные данные коллектабла (Mass, Collect/Release)

---

## События (C# Action)

- Только `event Action<T>`. **Никогда UnityEvent.**
- Всегда отписываться в `OnDisable` (или `OnDestroy` если уместнее).
- **Никогда анонимные лямбды:**

```csharp
// ПЛОХО:
_detector.Detected += (collectable, transform) => { ... };

// ХОРОШО:
private void OnEnable()
{
    _detector.Detected += OnCollectableDetected;
}

private void OnDisable()
{
    _detector.Detected -= OnCollectableDetected;
}

private void OnCollectableDetected(ICollectable collectable, Transform collectableTransform)
{
    // ...
}
```

---

## Unity-Specific

- `TryGetComponent` вместо `GetComponent` + null check
- TextMeshPro вместо legacy Text
- Coroutines для простых timed gameplay (но предпочтительно UniTask)
- ScriptableObject только если оправдан (конфиги, данные)
- **Минимизировать аллокации в Update**
- Избегать лишних Update-методов
- `SerializeField` только для значений, которые настраивает дизайнер
- НЕ exposить public fields, если не нужно
- `[RequireComponent]` где есть жёсткая зависимость от компонента на том же GO
- `sealed class` для классов, которые не наследуются
- `OverlapSphereNonAlloc` вместо `OverlapSphere` (нет аллокаций)
- Кэшировать buffer-массивы для NonAlloc-методов

---

## Async — UniTask

UniTask — предпочтительный async-инструмент. Не Task, не корутины для сложного.

### Канонический паттерн с cancellation

```csharp
private async UniTaskVoid SomethingAsync()
{
    CancellationToken cancellationToken = this.GetCancellationTokenOnDestroy();

    try
    {
        await DoWorkAsync(cancellationToken);
    }
    catch (OperationCanceledException)
    {
        return;
    }

    DoFinalize();
}

private async UniTask DoWorkAsync(CancellationToken cancellationToken)
{
    while (someCondition)
    {
        cancellationToken.ThrowIfCancellationRequested();

        await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
    }
}
```

### Правила UniTask

- Fire-and-forget — `.Forget()`
- Возвращаемый тип fire-and-forget методов — `UniTaskVoid`
- Cancellation через `this.GetCancellationTokenOnDestroy()` для авто-отмены при уничтожении
- `OperationCanceledException` ловить явно
- `UniTask.Yield(PlayerLoopTiming.Update, cancellationToken)` вместо `yield return null`

### Coroutines (если уж нужны)

- Кэшировать YieldInstruction:

```csharp
private static readonly WaitForSeconds OneSecondWait = new WaitForSeconds(1f);
```

---

## Канонический пример класса

```csharp
public class Player : MonoBehaviour
{
    private Mover _mover;
    private Rotator _rotator;
    private PlayerInputHandler _inputHandler;

    private bool _isSetupFinished;

    private void Update()
    {
        if (_isSetupFinished == false)
        {
            return;
        }

        _rotator.Rotate(_inputHandler.MouseDelta);
        _mover.Move(_inputHandler.MoveDirection);
    }

    public void Setup(Mover mover, Rotator rotator, PlayerInputHandler inputHandler)
    {
        _mover = mover;
        _rotator = rotator;
        _inputHandler = inputHandler;

        _isSetupFinished = true;
    }
}
```

---

## Чек-лист перед отдачей кода

1. Нет `var` — всё с явными типами
2. Нет комментариев
3. Нет анонимных лямбд при `+=`
4. Все события отписываются
5. Имена переменных описательные (не `t`, не `smooth`, не `c`)
6. `== false` вместо `!`
7. Braces на новой строке, везде используются
8. Пустые строки между логическими блоками
9. `sealed` где уместно
10. `[SerializeField] private` для редактора, не публичные поля
11. `TryGetComponent` вместо `GetComponent` + null check
12. SRP — класс делает одну вещь
13. Нет лишних абстракций / интерфейсов / managers
14. Нет лишних using-директив
15. Один класс в одном файле

---

## Если AI не знает как сделать

1. **Не выдумывать Unity API.** Не существует — спросить или поискать актуальную доку.
2. **Не предполагать ответы на скипнутые вопросы.** Жди явного ответа.
3. При сложной задаче — короткий план, реализация по шагам.
4. Если разработка идёт долго — можно вести progress.txt в рабочей папке, но без спама.

---

## Поведенческий итог

1. Делай быстро, делай минимально
2. Не делай ничего "на потом"
3. Не объясняй то, о чём не спросил
4. Не комментируй код
5. Если скипнул вопрос — жди ответа
6. Уважай SRP, KISS, YAGNI
7. Один класс — одна ответственность — один файл
