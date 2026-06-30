# AI Context: Mad Slime

> Файл контекста для AI-агента. Описывает фактическое состояние кода на 30.06.2026: что есть, как связано, какие слабые места, что в TODO. Документ синхронизирован с реальным содержимым `Assets/Scripts/`, не с устаревшими CLAUDE.md/AI_CONTEXT.md.

---

## Проект

- **Название:** Mad Slime
- **Тип:** Аркадная мобильная игра, low-poly 3D
- **Engine:** Unity 2022.3.62f2 LTS
- **Target:** mobile-first. NonAlloc, без LINQ в Update, без GC-аллокаций в hot path
- **Code style rules:** глобальный `AI_RULES.md` (см. `~/.config/opencode/AGENTS.md` + корневой `AI_RULES.md`)
- **Стек:** UniTask, New Input System, TextMeshPro, YG2 (плагин сохранений), AudioMixer. Без DI/ECS/event bus/singleton.

---

## Структура `Assets/Scripts/` (75 файлов)

```
Audio/                          (11) — AudioMixerController + 10 one-shot/loop player'ов
Camera/                         (1)  — CameraFollow
Collector/                      (2)  — Collector, ItemDetector
Detectors/                      (3)  — GenericOverlapDetector<T>, TargetSensor, AttractableDetector
Enemy/                          (4)  — Enemy, Wander, Chaser, Attacker  (namespace NPC.Enemy)
Game/                           (5)  — Timer, Pauser, LevelTransitor, GameplaySessionHandler, FillSessionHandler
Health/                         (2)  — Health, Healer  (namespace Assets.Scripts.HealthSystem — НЕ типично)
Interfaces/                     (3)  — ITarget (Interfaces ns), IMassHolder+IAttractable (глобал)
Inventory/                      (1)  — Inventory (тонкая обёртка над YG2.saves)
Item/                           (1)  — Item
Movement/                       (2)  — Mover, Rotator
Player/                         (8)  — Player, PlayerMass, LevelScaler, SprintSkill, AttractSkill, DodgeSkill, SkillTracker, SkillId, ItemTier
PlayerInput/                    (3)  — PlayerInputReader + 2 авто-генерированных
Quota/                          (2)  — QuotaTracker (глобал), QuotaEntry (ns Quota)
Saves/                          (3)  — SavesYG, SavesYG_Audio, SavesYG_Level (partial YG)
Scriptables/                    (4)  — ItemDefinition, SkillsConfig, SkillDefinition, TierScalerConfig
ShapeFill/                      (5)  — ShapeFillOrchestrator, ShapeFiller, GridBuilder, FlyingCube, FillCounter
UI/                             (7)  — HealthUI, TimerUI, MassUI, QuotaUI, QuotaPlateUI, SkillsPanel, GameplayUIFabric, FillUIFabric
UI/Menu/                        (8)  — BaseWindow, PauseMenu, ShopMenu, SkinsMenu, LeaderboardMenu, WinMenu, FailMenu, DeathMenu
```

---

## Ключевые абстракции и их связи

Формат: `[RC]` = `[RequireComponent]`, `[S]` = `Setup(…)`, `→ ev` = испускает `event Action`, `← ev` = подписан.

### Player (`Player/Player.cs`)
- `[RC]: CharacterController, Mover, Rotator, PlayerMass, Health, Healer`
- `[S]:` нет (всё через SerializeField)
- `[SF]: PlayerInputReader, QuotaTracker, Collector, Inventory, SprintSkill`
- `→ ev:` нет
- `← ev: Collector.ItemCollected, PlayerInputReader.SprintPerformed`

### Enemy (`Enemy/Enemy.cs`)
- `[RC]: Wander, Chaser, TargetSensor` (НЕ требует `Attacker` — Attacker живёт автономно на том же GO, но без явной связи)
- `[S]:` нет
- `→ ev:` нет
- `← ev: TargetSensor.TargetEntered`

### Health (`Health/Health.cs`)
- `[RC]:` нет
- `[SF]: int _maxValue, int _value, Timer _timer, float _invulnerabilityWindow, DodgeSkill _dodgeSkill, SkillTracker _skillManager`
- `→ ev: Died, Damaged, DamageDodged, ValueChanged(int), InvulnerabilityEnded`
- `← ev: Timer.Finished`
- **Неймспейс: `Assets.Scripts.HealthSystem`** (см. слабые места)

### Healer (`Health/Healer.cs`)
- `[RC]:` нет, авто-get `Health` на том же GO
- `[SF]: Health _health, Timer _timer, float _regenDelay`
- `→ ev:` нет
- `← ev: Health.InvulnerabilityEnded, Health.Died, Timer.Finished`
- **Неймспейс: `Assets.Scripts.HealthSystem`**

### PlayerMass (`Player/PlayerMass.cs`)
- `[RC]:` нет
- `[SF]: int _mass, int _defaultMass, Health _playerHealth, int _massPickupDivisor, TierScalerConfig _tierConfig`
- `→ ev: Action<int,int> Changed` (previous, current)
- `← ev:` никто (Player.OnItemCollected → Add)
- **Глобальный namespace, класс НЕ sealed** (см. слабые места)
- `Setup(int)` — мёртвый метод

### LevelScaler (`Player/LevelScaler.cs`)
- `[RC]:` нет
- `[SF]: PlayerMass, Transform _transfromToScale (опечатка!), ItemDetector, AttractableDetector, TierScalerConfig, float _smoothTime`
- `→ ev:` нет
- `← ev: PlayerMass.Changed`
- **Неймспейс: `Skills`** (для enum'а `ItemTier`)

### SprintSkill / AttractSkill (`Player/`)
- `SprintSkill`: namespace `Skills`, sealed. На `Timer.Finished` переключается Active/Cooldown. События `Started/Ended/CooldownEnded`. Подключён к `Player` через `PlayerInputReader.SprintPerformed` + `SkillsPanel.OnSprintButtonClick`.
- `AttractSkill`: namespace `Skills`, sealed. На `AttractableDetector.Detected` + `Timer.Finished`. **Никем не активируется кроме `SkillsPanel.OnAttractButtonClick` — нет input action, нет Player-ссылки.** (см. слабые места)

### DodgeSkill (`Player/DodgeSkill.cs`)
- Глобальный namespace, sealed, 11 строк
- `TryDodge()` через `Random.value < _dodgeChance`
- Зовётся из `Health.TryApplyDamage`

### SkillTracker (`Player/SkillTracker.cs`)
- namespace `Skills`, sealed
- `[SF]: SkillsConfig _config`
- `→ ev: Action<SkillId> SkillUnlocked`
- `← ev:` никого (стартует в `Start()`, эмитит per-skill)
- `IsUnlocked(SkillId)` — синхронный запрос

### Collector (`Collector/Collector.cs`)
- `[RC]:` нет
- `[SF]: MonoBehaviour _massHolderSource` (кастует к `PlayerMass` через `TryGetComponent`), `ItemDetector _detector`, `float _absorptionDuration`
- `→ ev: Action<Item.Item> ItemCollected`
- `← ev: ItemDetector.Detected`

### ItemDetector / AttractableDetector (`Collector/`, `Detectors/`)
- Наследники `GenericOverlapDetector<T>`. **Оба глобальный namespace.**

### GenericOverlapDetector<T> (`Detectors/GenericOverlapDetector.cs`)
- abstract, глобальный namespace
- `Update` → `OverlapSphereNonAlloc` → `Detected?.Invoke(target)` **для КАЖДОГО collider в радиусе КАЖДЫЙ кадр** (см. слабые места)
- `SetRadius(float)` бросает на negative

### TargetSensor (`Detectors/TargetSensor.cs`)
- namespace `Detectors`, sealed
- Ищет `ITarget` в `Collider[4]`, игнорирует `Health.IsAlive == false`
- `→ ev: TargetEntered(ITarget), TargetExited()` — стреляет только на переходе in/out range, не дребезжит
- `IsTargetInRange, DetectedTarget` — свойства

### Attacker (`Enemy/Attacker.cs`)
- namespace `NPC.Enemy`, sealed
- `[SF]: TargetSensor _sensor, int _damage, float _cooldown, Timer _timer`
- `→ ev: Action AttackPerformed`
- `← ev: Timer.Finished`
- Автономный (Update), использует `target.Health.TryApplyDamage(_damage)`

### Chaser / Wander (`Enemy/`)
- `Chaser`: глобал, НЕ sealed, 16 строк. `Tick(playerPos)` → `Move + Rotate`. Нет Awake-валидации.
- `Wander`: namespace `NPC.Enemy`, sealed, `[RC]: Timer`. Зона, idle. Подписан на `Timer.Finished`.

### Mover / Rotator (`Movement/`)
- `Mover`: sealed, `[RC]: CharacterController`, глобал. `SimpleMove` + `SmoothDamp`. `SetSpeedMultiplier/ResetSpeed` + `SpeedChanged` event.
- `Rotator`: sealed, глобал. Y-only `LookRotation` + `RotateTowards`.

### Item / ItemDefinition (`Item/`, `Scriptables/`)
- `Item`: namespace `Item`, sealed, `[SF]: ItemDefinition _definition, Collider _collider`. Реализует `IAttractable`. `Initialize/Collect/Shutdown`. **Один collider** — см. TODO #11.
- `ItemDefinition`: namespace `Item`, ScriptableObject. `displayName, previewModel, baseMass, tier`.

### Timer / Pauser / LevelTransitor (`Game/`)
- `Timer`: namespace `Game`, sealed. UniTask-цикл в `Update`-timing. `Setup/StartCount/Stop/Continue`. `→ ev: Finished, Ticked(float)`. Уважает `Time.timeScale` (не тикает на паузе).
- `Pauser`: namespace `Game`, sealed, `[DisallowMultipleComponent]`. Counter-based `RequestPause/RequestResume` → `Time.timeScale=0/1`.
- `LevelTransitor`: namespace `Game`, sealed. `Restart/LoadPrevious/LoadNext` через `SceneManager`.

### GameplaySessionHandler (`Game/GameplaySessionHandler.cs`)
- namespace `Game`, sealed
- `[SF]: Health, Healer, LevelTransitor, Timer, float _timerDuration, QuotaTracker, PlayerInputReader, Pauser`
- `→ ev: PlayerDied`
- `← ev: MovementKeyPressed (Begin), Timer.Finished, QuotaTracker.QuotaCompleted, Health.Died`
- `Revive() = Healer.Heal() + Health.TurnOnInvulnerabilityWindow(5f)`
- `Awake`: парсит `SceneManager.GetActiveScene().name` → `YG2.saves.CurrentLevel` (если имя `Level{N}`)

### FillSessionHandler (`Game/FillSessionHandler.cs`)
- namespace `Game`, **НЕ sealed**
- `[SF]: ShapeFillOrchestrator, LevelTransitor, Pauser`
- `→ ev: Win, Failed`
- `← ev: ShapeFillOrchestrator.FillCompleted` (подписка в `Start()`)
- `LoadNextLevel/LoadPreviousLevel` → `LevelTransitor` + `ClearInfo` (обнуляет YG2.saves.TargetQuotaCount/QuotaCount/DefaultCount)

### QuotaTracker / QuotaEntry (`Quota/`)
- `QuotaTracker`: глобал, sealed. `[SF]: List<QuotaEntry> _quota`. `→ ev: QuotaCompleted, QuotaChanged(int remaining, QuotaEntry)`. Метод `RegisterCollected` через `LINQ.All(...)` (см. слабые места).
- `QuotaEntry`: namespace `Quota`, sealed `[Serializable]`. `Definition, TargetCount, Collected, Remaining`. Конструктор валидирует.

### ShapeFill (`ShapeFill/`)
- `ShapeFillOrchestrator`: namespace `ShapeFill`, sealed. `[RC]: GridBuilder, ShapeFiller, FillCounter`. Оркестратор. `StartFill()` подписывается на `ShapeFiller.FillCompleted` (повторный вызов дублирует подписку). `→ ev: Action<float> FillCompleted`.
- `ShapeFiller`: namespace `ShapeFill`, sealed. Спавнит `FlyingCube` кубики через `UniTask.Delay` интервал. `→ ev: FillCompleted(float), CubeArrived(FlyingCube)`. `BuildShape` + `Fill(cubesCount)`. `target <= 0` → сразу `FillCompleted(0f)`.
- `GridBuilder`: namespace `ShapeFill`, sealed. Читает `_shapeTexture.GetPixels()` (аллокация), делит на grid (`_gridResolution`), классифицирует border/fill. `→ BorderCells, FillCells, Width, Height, ShapeTexture, CellSize, GetPixelColor, GridToWorld`.
- `FlyingCube`: namespace `ShapeFill`, sealed. `Launch(target, duration)` + `Update` интерполяция. `→ ev: Arrived(FlyingCube)`.
- `FillCounter`: **глобал**, sealed. Делит `defaultCount / _defaultCountDivisor` (см. слабые места). Переводит overage в `YG2.saves.Balance`.

### Audio (`Audio/`)
- `AudioMixerController`: namespace `Audio`, sealed. Мост `YG2.saves` ↔ `AudioMixer`. Exposed params: `MusicVolume`, `SFXVolume`. Методы `SetMusicVolume/SetSFXVolume` → clamp → saves → `ApplyMusic/SFX` → `YG2.SaveProgress()`. Подписан на `YG2.onGetSDKData`.
- Все 10 one-shot/loop плееров: namespace `Audio`, sealed, `[RequireComponent(AudioSource)]`. Повторяют boilerplate (см. DRY в слабых местах).
- `PlayerDamageSound`, `PlayerDeathSound`, `PlayerDodgeSound` используют `Health` (using `Assets.Scripts.HealthSystem`).

### Camera (`Camera/CameraFollow.cs`)
- Глобал, sealed. `LateUpdate` SmoothDamp. `_offset` фиксируется в `Awake` (см. слабые места).

### PlayerInput (`PlayerInput/`)
- `PlayerInputReader`: namespace `PlayerInput`, sealed. Обёртка над `PlayerInputActions`. `→ ev: MovementKeyPressed, SprintPerformed`.
- `PlayerInputActions.cs` — авто-генерированный, version 1, actions: `Move` + `Sprint`. **Реально используется.**
- `inputActions.cs` — авто-генерированный, version 0, **пустые maps**. **Мёртвый код.**

### Saves (`Saves/`)
- 3 partial-класса в namespace `YG`. `SavesYG`: `QuotaCount, DefaultCount, TargetQuotaCount, Balance`. `SavesYG_Audio`: `musicVolume=0.8, sfxVolume=0.8`. `SavesYG_Level`: `CurrentLevel=1`. **Публичные поля** (YG2 convention).

### UI (`UI/`)
- `HealthUI`, `TimerUI`, `MassUI`, `QuotaUI`, `QuotaPlateUI`, `SkillsPanel` — все `namespace UI`, sealed.
- `GameplayUIFabric` (gameplay-сессия: Pause/Shop/Skins/Leaderboard/Death меню), `FillUIFabric` (fill-сессия: Pause/Win/Fail меню) — `namespace Assets.Scripts.UI`, **НЕ sealed**. Подписаны на `Button.onClick` в `Awake`/`OnEnable`.
- `Menu/`: `BaseWindow` (база, не sealed, паузит через `Pauser` в `Initialize` + `RequestResume` в `OnDisable`). `PauseMenu` sealed, наследует `BaseWindow`. `ShopMenu`/`SkinsMenu`/`LeaderboardMenu` — **НЕ sealed**, но наследуют `BaseWindow` (идентичны по 29 строк). `WinMenu`/`FailMenu`/`DeathMenu` — **не наследуют `BaseWindow`**, дублируют логику (см. слабые места).

### Scriptables (`Scriptables/`)
- `ItemDefinition` — ns `Item`, SO.
- `SkillsConfig` — ns `Scriptables`, SO, `List<SkillDefinition>` + `IReadOnlyList` getter.
- `SkillDefinition` — ns `Scriptables`, sealed `[Serializable]`. `SkillId id, int requiredLevel`.
- `TierScalerConfig` — ns `Scriptables`, SO. `List<TierThreshold>` (вложенный). `GetUnlockedTier(int mass)` + `GetScaleFor(ItemTier)` (см. TODO #13).

---

## Что готово (можно играть)

- ✅ Движение игрока (Mover + Rotator, Y-only поворот, CharacterController)
- ✅ Поглощение предметов (Collector + ItemDetector → AnimateAbsorptionAsync → PlayerMass.Add → LevelScaler визуально)
- ✅ Притяжение предметов (AttractSkill + AttractableDetector, активируется через `SkillsPanel` кнопкой — нет input action)
- ✅ Рост игрока (линейный по массе, `LevelScaler` + `TierScalerConfig`)
- ✅ HP + i-frames + реген (после окончания i-frames, через Healer)
- ✅ Dodge (шанс через `DodgeSkill`, шлёт `Health.DamageDodged`)
- ✅ Враги: wander + chase + attack (`Enemy` + `Wander` + `Chaser` + автономный `Attacker`)
- ✅ TargetSensor (поиск игрока по `LayerMask`, дребезг-фильтр на `TargetEntered/Exited`)
- ✅ UI HP/таймера/массы/квоты
- ✅ Quota (квотные предметы учитываются, события `QuotaCompleted` ведут в `GameplaySessionHandler`)
- ✅ SessionHandler для gameplay-сессии (пауза на старте, старт по первому вводу, timeout/выполнение квоты → `LoadNext`, ревайв через `Healer.Heal() + Health.TurnOnInvulnerabilityWindow(5)`)
- ✅ Звук: `AudioMixerController` (YG2.saves ↔ AudioMixer), `LevelMusicPlayer` loop, 6+ one-shot реакций на игровые события
- ✅ Pause/Shop/Skins/Leaderboard/Death меню (`GameplayUIFabric` спавнит prefab'ы)

## Что не готово

- ❌ Post-level fill-сессия (`ShapeFillOrchestrator` + `FillSessionHandler` + Win/Fail меню через `FillUIFabric`) — есть, но в продакшене не подключена (см. `GameplaySessionHandler` грузит `LoadNext` напрямую, минуя fill).
- ❌ Главное меню (`UI/GameplayUIFabric` спавнит 4 кнопки — но нет «старт/выход» экрана)
- ❌ Кнопка смены языка в `PauseMenu` (TODO #1)
- ❌ Дебаффы от врагов, DeathZone
- ❌ Метапрогрессия (валюта = `Balance` есть в `SavesYG`, но нет магазина/навыков/ребитха)
- ❌ Префабы/ассеты/уровни (только `Test.unity`)

---

## СЛАБЫЕ МЕСТА ПРОЕКТА (главный раздел для рефактора)

Сгруппировано по приоритету. Каждый пункт содержит файл:строку и описание.

### A. Баги (порядок по убыванию срочности)

**A1. `Health/Healer.cs:58` — `if(_health.Value>=_health.MaxValue)` без пробелов и без braces.**
```csharp
if(_health.Value>=_health.MaxValue)  // нарушение: пробелы вокруг >=, ВСЕГДА braces
{
    return;
}
```

**A2. `Player/PlayerMass.cs:28-29` — `if (mass < 0)` без braces на throw.**
```csharp
if (mass < 0)
    throw new ArgumentOutOfRangeException(...);  // нарушение: ВСЕГДА braces
```

**A3. `Player/PlayerMass.cs:40-41` — `Add()` бросает exception с сообщением про `Decrease` (метода Decrease не существует).**
```csharp
throw new ArgumentOutOfRangeException(nameof(amount),
    "PlayerMass.Decrease requires amount to be non-negative...");  // ← Decrease? Метод Add.
```

**A4. `Player/PlayerMass.cs:26-35` — мёртвый `Setup(int)` с кривым контрактом.**
- Никто не вызывает.
- `Changed?.Invoke(_defaultMass, _mass)` — `previous` всегда равен `_defaultMass` (константа), сигнатура `(previous, current)` врёт.
- **Удалить.**

**A5. `Player/LevelScaler.cs:10` — опечатка `_transfromToScale` (везде в файле, 3 использования).**
- Переименовать в `_transformToScale`.

**A6. `Detectors/GenericOverlapDetector.cs:18-31` — `Detected?.Invoke(target)` срабатывает КАЖДЫЙ кадр для КАЖДОГО collider в радиусе.**
- В `ItemDetector` (наследник) это приводит к многократному запуску `AbsorbAsync` пока Item в радиусе.
- В `AttractableDetector` — менее критично, но всё равно лишние вызовы.
- **Нужен дедуп по `_lastDetected` HashSet.**

**A7. `ShapeFill/ShapeFillOrchestrator.cs:24-37` — `StartFill()` повторно подписывается на `ShapeFiller.FillCompleted`.**
- `OnDisable` отписывается, `StartFill` подписывается снова.
- Если кто-то дёрнет `StartFill` дважды без disable — двойной вызов `FillCompleted` в `OnFillCompleted` (но `FillSessionHandler` подписан один раз, поэтому ОК). Потенциальная ловушка.

**A8. `ShapeFill/FillCounter.cs:16` — `defaultCount /= _defaultCountDivisor` без проверки divisor != 0.**
- Если `_defaultCountDivisor == 0` → `DivideByZeroException` в рантайме.

**A9. `UI/MassUI.cs:11-25` — race condition.**
- `OnEnable` подписывается на `_playerMass.Changed`.
- `Start` обновляет текст по `_playerMass.Mass`.
- Если `PlayerMass.Changed` событие прилетит между `OnEnable` и `Start` — UI покажет устаревшее значение. Нужен `Awake`-валидация + чтение `Mass` в `OnEnable` сразу.

**A10. `Player/SkillTracker.cs:22-46` — `Start()` эмитит `SkillUnlocked` для всех разблокированных.**
- Если `SkillsPanel.OnEnable` выполнится ПОСЛЕ `SkillTracker.Start` — кнопки останутся скрытыми.
- **Решение:** переделать `SkillsPanel` на чтение `IReadOnlyCollection<SkillId> Unlocked` синхронно в `OnEnable` + подписку для инкрементальных обновлений.

**A11. `UI/QuotaUI.cs:16, 43-55` — `_nextPlateIndex` не сбрасывается в `OnDisable`.**
- При повторном включении скрипта на той же сцене plates начнут с неправильной Y-позиции.
- Решение: сбросить в `OnDisable` или вообще перейти на layout group.

**A12. `Player/AttractSkill.cs:8-94` — `Activate()` зовётся ТОЛЬКО из `SkillsPanel.OnAttractButtonClick`.**
- Нет input action, нет `Player`-интеграции, нет кнопки на мобильном джойстике.
- Скилл существует, но скрыт за одной кнопкой в UI.

**A13. `Camera/CameraFollow.cs:12-21` — `_offset` фиксируется в `Awake`.**
- Если target destroyed в рантайме — `LateUpdate` продолжит читать `null` (с `NullReferenceException`).
- Нужен `if (_target == null) return;` или подписка на destroy.

**A14. `Item/Item.cs:9` — один `[SerializeField] Collider _collider`.**
- Если у Item compound collider (несколько на child GO) — `Collect()` выключит только один. **TODO #11.**

**A15. `Health/Health.cs:7-153` — namespace `Assets.Scripts.HealthSystem`.**
- Нетипичный namespace с префиксом `Assets.Scripts.`. Должен быть `Health` (как папка).
- Затрагивает: `Healer.cs`, `Player.cs:1` (using), `ITarget.cs:1` (using), `UI/HealthUI.cs:1`, `Audio/PlayerDamageSound.cs:2`, `Audio/PlayerDeathSound.cs:2`, `Audio/PlayerDodgeSound.cs:2`, `GameplaySessionHandler.cs:1`.

**A16. `Quota/QuotaTracker.cs:5, 52` — `using System.Linq; _quota.All(entry => ...)`.**
- Delegate-аллокация при каждом `RegisterCollected` (хоть и мелкая).
- Заменить на for-loop.

**A17. `UI/GameplayUIFabric.cs:27-35` + `FillUIFabric.cs:22-28` — `Button.onClick.AddListener` в `Awake`/`OnEnable`.**
- Если component добавляется в рантайме после `Awake` — кнопки не работают.
- Если `_sessionHandler` подписка срабатывает в `Awake`, а сам handler ещё не инициализирован — NRE. Сейчас `OnEnable` для fabric, handler инициализируется в свой `Awake` — порядок не гарантирован.

**A18. `UI/Menu/FailMenu.cs:11` — единственный комментарий в коде.**
```csharp
[SerializeField] private Button _nextLevelButtonForADS;//тут будет предложение пройти уровень за рекламу
```
- Нарушение `AI_RULES.md` (нет комментариев). Удалить.

**A19. `Player/LevelScaler.cs:7-114` — Awake бросает `InvalidOperationException` на 5 несвязанных полей с одинаковым текстом.**
- 5 одинаковых блоков throw. Должна быть общая `ValidateSerializedFields()` или хотя бы общий helper.

**A20. `Movement/Mover.cs:26-31` — `SetSpeedMultiplier(multiplier <= 0)` молча early-return.**
- Лучше бросать `ArgumentOutOfRangeException` (как в `Timer.Setup`).

**A21. `Audio/UIButtonSound.cs:25-29` — бросает `InvalidOperationException` если массив пуст в `Awake`.**
- Не инициализирует `_source.outputAudioMixerGroup`/`playOnAwake = false` до этой проверки. Если бросит — `_source` останется дефолтным (playOnAwake = true).

**A22. `Game/FillSessionHandler.cs:17-21` — подписка в `Start()`, не `OnEnable`.**
- Если MonoBehaviour disable/enable — повторной подписки не будет. Сейчас не критично (вызывается один раз), но нарушает конвенцию остальных.

**A23. `Player/PlayerMass.cs:7` — класс НЕ sealed, в глобальном namespace.**
- Никто не наследует, ни от кого не зависит. Должен быть `sealed` + namespace `Player` или `Mass`.

**A24. `Player/Player.cs:8-13` — `Player` требует `CharacterController` жёстко.**
- Из-за этого `Mover` (своим `[RC]: CharacterController`) форсирует CC на ВСЕ сущности с `Mover` — включая `Enemy`/`Wander`/`Chaser` (TODO #5).
- Все NPC получают `CharacterController`, что анти-паттерн для мобов.

**A25. `Player/AttractSkill.cs:48` — `Activate` вызывает `IsUnlocked(SkillId.Attract)` через `_skillManager`, но `_skillManager` может быть null.**
- Нет Awake-валидации. Если не задан в инспекторе — `NullReferenceException` при первой попытке активации.

---

### B. Архитектурный долг (по убыванию важности)

**B1. Дубликаты Audio-классов (DRY).**
- 10 файлов повторяют boilerplate:
```csharp
_source = GetComponent<AudioSource>();
_source.outputAudioMixerGroup = _group;
_source.playOnAwake = false;
```
- Извлечь `AudioOneShotPlayer` (one-shot) и `AudioLoopPlayer` (loop) базовые классы. Сэкономим ~50 строк.

**B2. Дубликаты Menu-классов (DRY).**
- `ShopMenu`/`SkinsMenu`/`LeaderboardMenu` идентичны по 29 строк (один `_closeButton` + `Initialize(Pauser)` + `Close`).
- Решение: generic `ClosableMenu : BaseWindow` с `[SF] Button _closeButton`. Удалить три файла.
- `WinMenu`/`FailMenu`/`DeathMenu` не наследуют `BaseWindow` и дублируют `RequestPause/RequestResume`/`Close` логику. Перевести на `BaseWindow` (уже есть).

**B3. Неймспейс-хаос (нет единого convention).**
| Файл | Сейчас | Должно быть |
|---|---|---|
| `Health/Health.cs`, `Healer.cs` | `Assets.Scripts.HealthSystem` | `Health` |
| `UI/GameplayUIFabric.cs`, `FillUIFabric.cs`, `UI/Menu/WinMenu/FailMenu/DeathMenu/ShopMenu/SkinsMenu/LeaderboardMenu.cs` | `Assets.Scripts.UI` | `UI` |
| `Player/DodgeSkill.cs` | глобал | `Skills` |
| `Player/PlayerMass.cs` | глобал | `Player` или `Mass` |
| `Inventory/Inventory.cs` | глобал | `Inventory` |
| `Camera/CameraFollow.cs` | глобал | `Camera` |
| `Movement/Mover.cs`, `Rotator.cs` | глобал | `Movement` |
| `Collector/Collector.cs`, `ItemDetector.cs` | глобал | `Collector` |
| `Detectors/AttractableDetector.cs` | глобал | `Detectors` |
| `Detectors/GenericOverlapDetector.cs` | глобал | `Detectors` |
| `Interfaces/IMassHolder.cs`, `IAttractable.cs` | глобал | `Interfaces` |
| `Scriptables/ItemDefinition.cs` | `Item` (лежит в `Scriptables/`) | перенести в `Item/` ИЛИ сменить ns на `Scriptables` |
| `Quota/QuotaTracker.cs` | глобал | `Quota` |
| `ShapeFill/FillCounter.cs` | глобал | `ShapeFill` |
| `Item/Item.cs` | `Item` | OK |
| `Scriptables/SkillsConfig.cs`, `SkillDefinition.cs`, `TierScalerConfig.cs` | `Scriptables` | OK |
| `Saves/*` | `YG` (YG2 plugin convention) | OK |
| `Player/Item.cs`, `Scriptables/ItemDefinition.cs` | оба `Item` (но в разных папках) | OK |

**B4. `Enemy/Chaser.cs:3` — глобальный namespace, не sealed, нет Awake-валидации.**
- Враг формально живёт в `NPC.Enemy`, а Chaser в глобале. Несогласованно.

**B5. `Enemy/Enemy.cs:8-11` — НЕ sealed, `[RC]` не включает `Attacker`.**
- Attacker живёт автономно, не в `RequireComponent` и не подключён к `Enemy` явно. Серый паттерн.

**B6. `Game/FillSessionHandler.cs:8` — НЕ sealed.**

**B7. `UI/GameplayUIFabric.cs:9`, `UI/FillUIFabric.cs:9` — НЕ sealed.**
- Они классы-фабрики, могут быть final.

**B8. `UI/Menu/ShopMenu.cs:8`, `SkinsMenu.cs:8`, `LeaderboardMenu.cs:8` — НЕ sealed.**

**B9. `UI/Menu/WinMenu.cs:8`, `FailMenu.cs:8`, `DeathMenu.cs:8` — НЕ sealed.**

**B10. `Player/AttractSkill.cs` не имеет input action.**
- Sprint подключён через `PlayerInputReader.SprintPerformed` + `SkillsPanel.OnSprintButtonClick`.
- Attract — только через `SkillsPanel.OnAttractButtonClick`. Нет мобильного hotkey.

**B11. `Quota/QuotaTracker.cs:11` — `List<QuotaEntry> _quota` хранится как public SerializeField (не через Setup).**
- Дизайнер заполняет в инспекторе. Это норм, но класс берёт зависимость от порядка: `Awake` инкрементит `TargetQuotaCount`, `Start` эмитит `QuotaChanged` для UI. Эти два момента создают два разных timing'a (race с `QuotaUI.OnEnable`).

**B12. `Health/Health.cs:119-131` — `TurnOnInvulnerabilityWindow(float time)` нарушает convention.**
- Метод `Healer.Revive()` вызывает его с константой `5f`. Эта константа дублируется в `Healer` и в `GameplaySessionHandler` (если бы `Healer.Revive()` её не звал — была бы и там).
- Сейчас: `Revive()` → `_healer.Heal() + _health.TurnOnInvulnerabilityWindow(5f)`. Heal() и так зовётся. Нужен просто `Revive()` без двух вызовов.

**B13. `Player/LevelScaler.cs:30-56` — 5 throw-блоков с одинаковым текстом.**
- Вынести в `RequireAllSerializedFields()`. Сравни с `Health.Awake` — там один throw.

**B14. `Player/PlayerMass.cs:7` — namespace глобал + НЕ sealed + мёртвый `Setup`.**
- См. A4 + B3.

**B15. `Item/Item.cs:8-9` — `ItemDefinition` + `Collider` через SerializeField, без Awake-валидации.**
- `Collect()` упадёт `NullReferenceException` если `_collider` не задан.

**B16. `Quota/QuotaTracker.cs:11-30` — `Awake` пишет в `YG2.saves.TargetQuotaCount`, `Start` эмитит `QuotaChanged`.**
- Нет единой точки истины: `YG2.saves` хранит агрегат, `_quota` хранит детали. UI читает из event, save читается из YG2 — два независимых состояния.

**B17. `Game/GameplaySessionHandler.cs:90-101` — `Begin()` стартует таймер на первом нажатии клавиши.**
- Это нужно, чтобы `Pauser.RequestPause` держал игру на паузе до первого ввода. Но это связывает session и input — лучше вынести «ждать первого ввода» в отдельный `WaitForFirstInput` компонент.

**B18. `Player/SprintSkill.cs:7` + `Player/AttractSkill.cs:8` — два скилла копируют FSM Active/Cooldown/Events.**
- Можно вынести базовый `TimedSkill` с FSM, наследники только переопределяют `OnStarted/OnEnded/OnTick`. Но YAGNI: два класса не повод абстрагировать. Оставить как есть.

**B19. `Scriptables/TierScalerConfig.cs:23-57` — TODO #13 (декомпозиция).**
- `List<TierThreshold>` хранит `tier + requiredMass + scaleMultiplier` в одном типе. Это «массовая» и «скейл» логика смешаны. Декомпозиция: `mass→tier` отдельно (LevelUpConfig), `tier→scaleMultiplier` отдельно (TierScaleConfig). Или ввести два SO.

**B20. `Audio/AudioMixerController.cs:25-47` — `Awake` читает `YG2.saves` напрямую без guard `YG2.isSDKEnabled`.**
- В `TryApplyFromSaves` есть guard, в `Awake` — нет. Может быть NRE в editor без SDK.

**B21. `Item/Item.cs:11` — `_defaultScale = Vector3.one` инициализируется в field initializer, не в `Initialize`.**
- Если в инспекторе `transform.localScale` будет изменён ДО первого `Initialize` — `Initialize` затрёт его. Потенциальный баг.

**B22. `Health/Health.cs:51-54` — `Start` стреляет `ValueChanged(_value)` однократно.**
- Это для UI инициализации. Конвенция: лучше `OnEnable` + проверка `_isFirstActivation`. Сейчас работает потому что UI подписан в своём `OnEnable` после `Awake` (если порядок корректный).

**B23. `Audio/FlyingCubeArrivalSound.cs:9-10` — `[RequireComponent(AudioSource)] [RequireComponent(ShapeFiller)]`.**
- Хорошо. Но поле `_filler` инициализируется в `Awake` (не `OnEnable`), что ок.

**B24. `Movement/Mover.cs:43-62` — `SimpleMove` без `Time.deltaTime` (это правильно, `SimpleMove` сам инжектит).**
- Не баг, но при чтении кода это выглядит подозрительно. Можно добавить комментарий… но в проекте нет комментариев. Оставить как есть.

**B25. `Game/FillSessionHandler.cs:34-38` + `52-57` — `ClearInfo` обнуляет YG2.saves внутри `LoadNext/LoadPrevious`.**
- Это side effect, не SRP. Должен быть отдельный `SaveReset` компонент.

**B26. `Player/Player.cs:80-92` — `ConvertToWorldDirection` использует `Vector3.forward`/`Vector3.right` без привязки к камере.**
- Это правильно для top-down игры без вращающейся камеры. Но если камера когда-то начнёт вращаться — сломается. Сейчас `CameraFollow._offset` фиксирован в `Awake` и камера не вращается вокруг таргета, так что ОК.

**B27. `Player/LevelScaler.cs:92-106` — `Update` каждый кадр делает `SmoothDamp` + `ApplyMultiplier` даже если изменения нет.**
- Уже есть early-return на `Mathf.Approximately`. Хорошо.

**B28. `Player/Player.cs:4` — `using Skills` (для `SkillId.Sprint`) в `OnSprintPerformed` нет (но `SprintSkill` сам фильтрует по unlocked).**
- Норм.

**B29. `Inventory/Inventory.cs:1-15` — TODO #10 (мёртвая абстракция).**
- Класс — 15 строк обёртки над `YG2.saves`. Не хранит список предметов, не имеет UI.
- **Спросить у ментора** (по тексту TODO). Пока — оставить, это не код-баг.

**B30. `Quota/QuotaTracker.cs:9` — sealed, глобал, `[SF] List<QuotaEntry>`.**
- Стандарт. ОК.

**B31. `Audio/AudioMixerController.cs:67-70` — guard `YG2.isSDKEnabled == false` → return. Но `ApplyMusic/ApplySFX` могут быть не вызваны — `_isReady` остаётся `false`.**
- После этого `SetMusicVolume` ничего не делает (early return). Корректно.

**B32. `PlayerInput/inputActions.cs` (version 0) — мёртвый файл.**
- Удалить `.cs` + `.inputactions` asset.

**B33. `UI/Menu/BaseWindow.cs:7` — `class` (не sealed).**
- Базовый, легитимно.

**B34. `Item/Item.cs:1-37` — нет Awake-валидации `_collider` и `_definition`.**
- `Collect()` и `Mass` упадут NRE если не заданы.

**B35. `Game/Pauser.cs:13-21` — counter-based, корректен. Но `RequestPause()` без guard `>= 1` дергает `Time.timeScale = 0` каждый раз.**
- Уже норм: `if (_pauseRequestCount >= 1)` стоит ПОСЛЕ инкремента, и это условие всегда true после первого вызова. Первый вызов: count=1, ставит timeScale=0. Второй: count=2, тоже ставит 0. Идемпотентно, но с лишним write'ом. Микро-оптимизация.

**B36. `Movement/Mover.cs:17-18` — публичные свойства `Velocity`, `ActualSpeed` нигде не используются.**
- Лишний API. Удалить или оставить (YAGNI — удалить, но не критично).

---

### C. Глобальный TODO (из `Assets/Resources/TODO/TODO.md`)

| # | Пункт | Статус | К чему относится |
|---|---|---|---|
| 1 | Кнопка смены языка в PauseMenu | ❌ | UI |
| 2 | При поглощении предметы не выключаются | ❌ (compound collider) | A14 |
| 3 | При аттракте тянутся гигантские предметы | ❌ (нет фильтра по `Definition.Tier`) + A12 | Item/AttractSkill |
| 4 | При аттракте ломается перемещение | ❌ | Movement/AttractSkill |
| 5 | Перенести NPC с CharacterController | ❌ | A24 |
| 6 | С ростом игрок должна отдаляться камера | ❌ | A13 (related) |
| 7 | UI главного меню не оффается при старте | ❌ | нет главного меню |
| 8 | У коллектора выделить анимацию поглощения | ❌ | Collector.cs AbsorbAsync |
| 9 | В HP выделить отдельный класс для инвула | ❌ | Health.TryApplyDamage |
| 10 | Спросить ментора про инвентарь | ❓ | B29 Inventory |
| 11 | Задумайся о надобности коллайдера | 🤔 | A14 |
| 12 | В LevelScaler разобраться зачем Setup | ✅ мёртвый Setup в PlayerMass, а в LevelScaler его нет — возможно путаница | A5 |
| 13 | TierScalerConfig декомпозировать | ❌ | B19 |

---

### D. Соответствие AI_RULES.md — общая сводка

- ✅ `var` — НЕ найдено
- ✅ `!bool` вместо `== false` — НЕ найдено
- ✅ `UnityEvent` — НЕ используется
- ✅ Анонимные лямбды в `+=` — НЕ используются (кроме авто-генерированного `PlayerInputActions`)
- ✅ `GameObject.Find` / `FindObjectOfType` / `Transform.Find` — НЕ найдено
- ✅ Singleton — НЕ используется
- ⚠️ Комментарии — **1 нарушение** (A18: `FailMenu.cs:11`)
- ⚠️ `sealed` — **~10 классов без `sealed`**: `Enemy`, `Chaser`, `PlayerMass`, `FillSessionHandler`, `GameplayUIFabric`, `FillUIFabric`, `ShopMenu`, `SkinsMenu`, `LeaderboardMenu`, `WinMenu`, `FailMenu`, `DeathMenu`. См. A23, B4-B9.
- ✅ `OnDisable`-отписки — везде где есть `OnEnable` есть `OnDisable`
- ✅ `OverlapSphereNonAlloc` — везде
- ⚠️ `==` vs `== false` — `Healer.cs:58` не имеет пробелов и braces (A1)
- ⚠️ `sealed` где уместно — перечислено в B4-B9
- ⚠️ LINQ в Update — только `QuotaTracker.cs:52` (A16), не в `Update`, но всё равно аллокация delegate

---

### E. Архитектурные паттерны, которые работают хорошо

- ✅ Composition через `[RequireComponent]` + `GetComponent` в `Awake`. Без singleton, без ServiceLocator.
- ✅ Только `event Action<T>`, никаких `UnityEvent`.
- ✅ OnEnable/OnDisable пара везде.
- ✅ TryGetComponent вместо GetComponent + null check.
- ✅ UniTask с `this.GetCancellationTokenOnDestroy()` в Timer, Collector.
- ✅ Counter-based Pauser (несколько окон могут звать RequestPause/Resume независимо).
- ✅ Sound — каждая реакция = отдельный компонент, не god-class.

### F. Архитектурные паттерны, которые хочется улучшить

- ⚠️ Audio — 10 классов с одинаковым boilerplate, нужен базовый.
- ⚠️ Menu — 3 дубликата + 3 не наследуют `BaseWindow`. Нужен `ClosableMenu` + переход Win/Fail/Death на `BaseWindow`.
- ⚠️ Health/Healer — namespace `Assets.Scripts.HealthSystem` нетипичен.
- ⚠️ Скиллы — `SprintSkill` и `AttractSkill` копируют FSM (YAGNI, но если добавится 3-й скилл — пора).
- ⚠️ `CollectableAttractor` (из старого CLAUDE.md) заменён на `AttractSkill` (работает только через `SkillsPanel` кнопку, не через input).
- ⚠️ Отсутствует `Prey` (`NPC/Prey/Fleer`/`Prey` — нет в коде, несмотря на GDD упоминание).

---

### G. TODO, не упомянутые в TODO.md, но обнаруженные при чтении

- G1. `Item/Item.cs` — нет Awake-валидации `_collider` и `_definition`. (B34)
- G2. `Item/Item.cs` — `_defaultScale` инициализируется в field initializer, не в `Initialize`. (B21)
- G3. `Player/Player.cs:8-13` — `Player` требует `CharacterController` жёстко, форсит CC на NPC. (A24)
- G4. `Player/Player.cs:80-92` — `ConvertToWorldDirection` без привязки к камере. (B26)
- G5. `Movement/Mover.cs:17-18` — `Velocity`/`ActualSpeed` свойства нигде не используются. (B36)
- G6. `Player/Player.cs:65-78` — `OnItemCollected` дёргает `_quotaTracker.IsQuotaItem` и `RegisterCollected` — оба метода внутри `QuotaTracker` ищут `definition` через `FindEntryIndex` (линейный поиск). При большом `_quota` — лишний цикл на КАЖДЫЙ собранный item.
- G7. `Audio/UIButtonSound.cs:25-29` — проверка `_buttons.Length <= 0` идёт ДО инициализации `_source` (`_source.outputAudioMixerGroup = _group; playOnAwake = false`). (A21)
- G8. `PlayerInput/inputActions.cs` — мёртвый файл. (B32)

---

## Тех. долг (финальный, краткий)

1. **Критичные баги (фиксить первыми):** A1, A2, A3, A4, A5, A6, A10, A11, A13, A14, A15, A18
2. **Архитектурный долг (фиксить в одну сессию):** B1, B2, B3, B4-B9 (sealed), B19, B25, B29
3. **Низкий приоритет (когда руки дойдут):** A7, A8, A9, A12, A16, A17, A19-A25, B10-B18, B20-B36, G1-G8

---

# GDD: Mad Slime

## 1. Анализ рефа

### 1.1. Краткое описание реф-игры
Аркадная мобильная игра в мультяшном low-poly стиле. Игрок управляет слаймом, который поглощает объекты в современных городских и интерьерных локациях. Основной прогресс строится на выполнении квоты уровня через поглощение определённых предметов. Сеттинг: современный город, квартиры, улицы, промышленные зоны. Визуальный стиль: low-poly, мультяшный.

### 1.2. Основная механика геймплея и управление
Основная механика: втягивание объектов слаймом и выполнение квоты уровня. Игрок управляет движением слайма. Игровой цикл: перемещение → втягивание → поглощение → поиск нужных объектов → квота → следующий уровень. В конце уровня все поглощённые объекты конвертируются в кубы/очки. Управление: телефон — джойстик + кнопки; ПК — клавиатура.

### 1.3. Дополнительные механики
- Квоты на уровень
- Время на прохождение
- Рост слайма внутри уровня
- Уровни, привязанные к размеру
- Разные типы объектов по размеру/ценности
- Движущиеся объекты (люди, животные, техника)
- Статичные объекты окружения
- Враги и опасные объекты
- Дебаффы от врагов
- Смерть при попадании в опасные зоны / большом уроне
- Валюта, навыки, ребитх

### 1.4. Притягательные аспекты
Простая core-механика, приятное втягивание, прогресс внутри уровня, чувство усиления через рост, поиск объектов, напряжение от таймера/врагов, разнообразие окружения, билды через навыки.

### 1.5. Промахи и риски
Перегрузка механиками, дисбаланс квоты/таймера, слабая читаемость при большом количестве предметов, однообразие без уровней, сложность балансировки экономики/ребитха.

## 2. ГДД планируемой игры

### 2.1. Описание игры
Игрок — слайм, появляется в квартире. Цель: поглощать, выполнять квоту, переходить на новые локации. Каждый уровень: квота, время, набор объектов, враги. Квотные предметы дают больше прогресса/награды. Пример: уровень 1 — канцелярия (ручки, карандаши, ластики). После времени: выполнил → прошёл + лишний прогресс → валюта; не выполнил → поражение.

### 2.2. Структура игры

**Старт:** короткий комикс о появлении слайма, затем уровень 1. Обучение минимальное.

**Цикл:** появился → получил квоту → перемещаешься → поглощаешь → ищешь нужные → избегаешь врагов → копишь прогресс → закончил уровень до окончания времени.

**Уровни (10):** квартира → дом → улица → парковка → промзона. Каждый = размер слайма + новые объекты + новая сложность.

**Поражение:** не выполнил квоту / много урона / DeathZone → рестарт уровня.

**Метапрогрессия:** валюта за уровни → навыки + скины + достижения.

**Навыки (3 ветки):**
- **Выживание:** +max HP, сопротивление урону, dodge
- **Мобильность:** +скорость, рывок
- **Поглощение:** +радиус притягивания, автопритягивание, тентакли

**Ребитх:** после прохождения игры активируется ребитх, увеличивает сложность + модификаторы (меньше еды, больше врагов). Награда: новые модификаторы, косметика.

### 2.3. Визуальная составляющая
Low-poly, мультяшный, упрощённые формы, яркие цвета, хорошая читаемость. Слайм: визуально растёт, деформируется при движении, эффекты втягивания. Окружение: квартиры, улицы, городские зоны, промзоны. Объекты: мелкие, бытовые, люди/животные, техника/транспорт, здания.

### 2.4. Монетизация
- **Интерстишиал-реклама:** после уровня, после поражения
- **Ревард-реклама:** возрождение, доп. валюта, временные бонусы (замедление врагов, увеличение награды, усиление притягивания, ускорение)
