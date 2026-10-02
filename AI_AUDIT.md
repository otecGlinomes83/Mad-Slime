# Аудит проекта по Wiki/AI_RULES.md

Дата: 2026-10-02.
Охват: все `.cs` в `Assets/MadSlime` (~180 файлов, ~17.6k строк), сцены Menu/Game/Fill/Shop, UI-префабы, DI-скоупы, Editor-тулы, Localization.asset, SettingsYG2, EditorBuildSettings, GUID-ссылки удалённых ассетов. `PlayerInputActions.cs` — автоген, вне правил.

## Оценка соответствия правилам: 7/10

Каркас выдержан строго: нет `var`, нет UnityEvent, события только `Action`, `== false` по всему коду, скобки/многострочность соблюдены, fail-fast гварды на каждом инжекте, явные зависимости через `Construct`/`SerializeField`, фабрики/пулы на месте, UniTask с cancellation по канону, отписки в `OnDisable` почти везде, сейв-контракт не тронут.

Оценку снижают: 4 нарушения, поименованных прямо в правилах, и не исправленных; ~40 строк комментариев в 8 файлах; 5 тернарников; 11 `Debug.Log` в рантайме; запись `Time.timeScale` мимо `Pauser`; магические числа/строки; 2 реальных бага; мёртвый код; дебаг-компонент в проде.

---

## 1. Баги (не «стиль», а неправильное поведение)

### 1.1 ModelPlacer — потерянный угол bounds (копипаст)
`Assets/MadSlime/Scripts/UI/Shop/ModelPlacer.cs:151-158`
В `TransformCornersToBounds` угол `[3]` дублирует угол `[1]` — записан `(+extents.x, -extents.y, -extents.z)` вместо `(+extents.x, +extents.y, -extents.z)`. Верхний угол бокса не инкапсулируется; при некоторых поворотах модели bounds занижены → превью в магазине стоит со смещением/неверным масштабом. В `RouletteWinPopup.ComputeWorldBounds` те же 8 углов записаны правильно.
Как чинить: заменить `[3]` на `new Vector3(+extents.x, +extents.y, -extents.z)`.

### 1.2 ShopLifetimeScope — двойная регистрация Wallet
`Assets/MadSlime/DI/ShopLifetimeScope.cs:44` и `:50` — `builder.RegisterComponent(_wallet)` вызван дважды. Работает «последний выигрывает», но это замаскированная ошибка сетапа: при смене инстанса на одной из строк второй молча перезапишет первый, а память о регистрация-дубле никто не найдёт.
Как чинить: удалить строку 50.

### 1.3 ItemPool — утечка словаря на уничтоженных предметах
`Assets/MadSlime/Scripts/Game/Level/ItemPool.cs:10,38`
`_prefabByInstance` наполняется на каждый `Get`. Несобранные предметы умирают вместе со сценой Game (дети `_itemsRoot`), но из словаря не исчезают никогда — за сессию накапливаются сотни мёртвых записей. Пул живёт в root-скоупе, чистки нет.
Как чинить: в начале `Generate()` у `LevelGenerator` прогонять `_spawnedItems` через `_itemPool.Release` перед `Clear()`, либо дать `ItemPool` метод `ClearDestroyed()` с проверкой `item == null`.

### 1.4 UiEnableAnimation — твин-лик в режиме Scale
`Assets/MadSlime/Scripts/UI/Animations/UiEnableAnimation.cs:73-80, 92-99`
В `PlayOutro`/`PlayIntro` сначала создаётся move-твин (`CreateMoveTween`), затем для режима `Scale` ссылка перезаписывается `DOScale`. Первый твин остаётся живым без ссылки и без `Kill` — интерполирует константу до конца, по одному утёкшему тину на каждое Show/Hide каждой панели.
Как чинить: в режиме `Scale` move-твин не создавать — ветвление до создания, а не перезапись ссылки.

### 1.5 LeaderboardMenu — виснет на «Загрузка…» при ошибке
`Assets/MadSlime/Scripts/UI/Windows/LeaderboardMenu.cs:85-89` + `Assets/MadSlime/Adapters/Yg2LeaderboardService.cs:21`
Подписан только success-callback `YG2.onGetLeaderboard`. При ошибке запроса текст остаётся `leaderboard_loading` навсегда — прямое нарушение платформенного правила «Лидерборд при ошибке показывает `leaderboard_error` и путь повтора». Ключ `leaderboard_error` в `Localization.asset` есть, но не используется нигде.
Как чинить: подписать `YG2.onErrorGetLeaderboard` в `Yg2LeaderboardService`, пробросить событием `EntriesFailed`, в `LeaderboardMenu` показывать `leaderboard_error` + повторную `RequestLeaderboard`.

### 1.6 Локализация «Leaderboard!»
`Assets/MadSlime/Resources/Prefabs/UI/Menu/LeaderboardMenu.prefab`
Заголовок окна — захардкоженный `Leaderboard!`; на префабе один `LocalizedText` (он на auth-кнопке), ключа для заголовка в `Localization.asset` нет. На ru/tr игроки видят английский заголовок. Нарушение «Любой пользовательский текст — ключ в Localization.asset».
Как чинить: ключ `leaderboard_title` (ru/en/tr) + `LocalizedText` на заголовке.

---

## 2. Нарушения, поименованные в самих правилах (не исправлены)

### 2.1 «Трио булевых вместо машины — не годится»
`Assets/MadSlime/Scripts/Game/GameplaySessionHandler.cs:29-31` — ровно тот набор `_isStarted/_isFinished/_isSubscribed`, который правила приводят как антипример. Состояний фактически четыре: WaitingStart, Running, Finished, Navigating.
Как чинить: `private enum SessionState { WaitingForStart, Running, Finished }` + один метод перехода; подписка/отписка — без флага, в `OnEnable`/`OnDisable`.

### 2.2 «CanSpinForAd не должен мутировать сейв»
`Assets/MadSlime/Scripts/UI/Roulette/RouletteService.cs:137-142, 144-149` — оба геттера вызывают `PruneAdSpins`, то есть читающий запрос изменяет список в сейве. Плюс `RouletteView.Update` дёргает их каждый кадр (`:521, :548`) — мутация состояния из Update.
Как чинить: `PruneAdSpins` — отдельным публичным вызовом на открытии рулетки/после `RegisterAdSpin`; геттеры делают только чтение (фильтр по времени без удаления).

### 2.3 «Мёртвый Timer.Continue()»
`Assets/MadSlime/Scripts/Game/Timer.cs:63-82` — 20 строк публичного API, использований ноль (grep по проекту). Правила называют его прямо.
Как чинить: удалить метод.

### 2.4 «Базовую скорость пишут два компонента»
`Assets/MadSlime/Scripts/Player/Player.cs:85` (`PlayerConfig.BaseMoveSpeed * upgrades`) и `Assets/MadSlime/Scripts/Player/LevelScaler.cs:105,130` (`tierSpeed * upgrades`). Правило «Одно значение состояния — один писатель» приводит эту пару как пример ошибки. `Player.Awake` пишет скорость, которую `LevelScaler.OnEnable` тут же перезаписывает.
Как чинить: убрать запись из `Player.Awake`; единственный писатель — `LevelScaler` (он знает тир и апгрейды).

---

## 3. Нарушения правил кода

### 3.1 Комментарии — 40 строк в 8 файлах
Запрещены безусловно. Удалить, при необходимости вынести смысл в имена:
- `Scripts/UI/Shop/ModelPlacer.cs:73-74, 122-124`
- `Scripts/UI/Roulette/RouletteReel.cs:51-53, 118-119, 294-302`
- `Scripts/UI/Roulette/RouletteService.cs:112-113`
- `Scripts/UI/Roulette/RouletteWinPopup.cs:271-272`
- `Scripts/UI/Roulette/RouletteView.cs:152-153, 233-237, 267-268, 280-281, 294-295, 354, 538`
- `Scripts/UI/Shop/ShopItemView.cs:94-95`
- `Scripts/UI/Shop/ShopPanel.cs:88-89, 358-359, 485-486`
- `Scripts/UI/MainMenu.cs:105-106`

### 3.2 Тернарники — 5 шт
Писать `if/else`:
- `Scripts/Player/Player.cs:116` — `isQuota == true ? QuotaMassMultiplier : MassMultiplier`
- `Scripts/Player/Player.cs:119` — `isQuota == true ? 1f : ForeignFillMultiplier`
- `Scripts/Game/Level/LevelConfigResolver.cs:39` — тернарник внутри интерполяции
- `Scripts/UI/Roulette/RouletteView.cs:296` — `pool.Count > 0 ? pool : AllSkinsSorted()`
- `Scripts/UI/Roulette/RouletteReel.cs:305` — `Approximately(delta,0f) ? 0f : Sign(delta)`

### 3.3 Debug.Log в рантайме — 11 мест
Правило: только fail-fast исключение или `SessionStateLogger`. Editor-тулы могут логать.
- `Scripts/Collectables/Collector.cs:74` — LogError вместо throw (catch: предмет без Definition — это битый сетап, игрок не виноват)
- `Scripts/Game/Level/ItemPool.cs:24` — LogWarning о выключенном префабе → throw в `Awake` скоупа
- `Scripts/Game/Level/LevelGenerator.cs:144, 168, 175, 281`
- `Scripts/Game/Level/QuotaGenerator.cs:44`
- `Scripts/Game/Level/LevelConfigResolver.cs:37`
- `Scripts/Game/GameplaySessionHandler.cs:190`
- `Scripts/Game/FillSessionHandler.cs:156, 163`
- `Scripts/Game/LeaderboardReporter.cs:46`

### 3.4 Time.timeScale пишет не только Pauser
- `Scripts/Game/Startup.cs:26` — `Time.timeScale = 1f;`
- `Scripts/Game/GameDirector.cs:105-108` — сброс в 1 перед загрузкой
Как чинить: метод `Pauser.ResetToPlay()` (сброс счётчика + timeScale с гвардом `IsPauseGame`), и звать его; в `GameDirector` — через переданный `Pauser` либо через событие скоупа. Сейчас прямая запись ещё и оставляет `_pauseRequestCount` старого Pauser несогласованным.

### 3.5 Расчёты внутри строки форматирования
`Scripts/UI/Roulette/RouletteView.cs:539` — `$"{freeRemainSeconds / 60}:{freeRemainSeconds % 60:00}"`. Правило: сначала `minutes`/`seconds`, потом строка.
```csharp
int minutes = freeRemainSeconds / 60;
int seconds = freeRemainSeconds % 60;
_spinPriceText.text = $"{minutes}:{seconds:00}";
```

### 3.6 Магические числа/строки
- `Scripts/UI/Roulette/RouletteReel.cs:408` — `Mathf.Min(_config.WinDwellSeconds, 0.5f)`; `:411` — `DOPunchScale(..., 4, 0.5f)` → const/SerializeField
- `Scripts/UI/Shop/UpgradeItemView.cs:156-163` — пороги `1000000`/`1000` в `FormatPrice` → const
- `Scripts/Player/TierResolver.cs:67` — fallback `return 4f;` недостижим (Awake кидает на пустой таблице) — удалить ветку целиком
- `Scripts/UI/Shop/ModelPlacer.cs:86` и `Scripts/UI/Roulette/RouletteWinPopup.cs:299` — `SetTrigger("Walk")` → общий const (или свойство на `SkinModel`)
- `Scripts/Core/Item.cs:31,160` — `_fadeTween` пишется и никогда не читается → удалить поле (Kill идёт через `SetTarget`)
- `Saves/SavesYG.cs:33` — `PreviousScene` в рантайме не пишется и не читается (только редакторский ресет) — мёртвое поле сейва; удалять аккуратно: ключ уйдёт из JSON
- `Scripts/ShapeFill/CubeSpawner.cs:50` — `Shader.PropertyToID("_Color")` на каждый спавн; в `Item.cs` ID кэшируются в `static readonly` — сделать так же

### 3.7 GetComponent / fail-fast
- `Scripts/Audio/UIButtonSound.cs:25` + null-check на `:39` — заменить на `TryGetComponent` (компонент требуется, но проверка есть — правило однозначно)
- `Scripts/UI/Roulette/RouletteWinPopup.cs:219` — `GetComponent<ParticleSystem>()` без проверки и без `TryGetComponent`: на префабе без системы — NRE в рантайме. Правило «Разыменование "повезёт — не повезёт" запрещено» → `TryGetComponent` + throw с именем префаба

### 3.8 sealed
- `Scripts/UI/Shop/ShopItemView.cs:10` — `public class` без наследников → `sealed`
- `Scripts/Core/Scriptables/Shop/ShopContent.cs:10` — то же

### 3.9 static-состояние
`Scripts/UI/MainMenu.cs:99` — `private static bool s_dailyShownThisSession;` в сценовом MonoBehaviour. Переживает выгрузку сцены, не привязано к жизненному циклу. Имя по правилам, но само состояние — нет.
Как чинить: перенести в `PlayerProgress`/сессионный сервис (поле `IsDailyShownThisSession`, выставляется при показе), либо в `GameDirector`.

### 3.10 Подписки без отписки
- `Scripts/Game/SessionStateLogger.cs:24` — `SceneManager.sceneLoaded +=` в `Start`, отписки нет вообще
- `Adapters/Yg2AdsService.cs:21-27`, `Adapters/Yg2LeaderboardService.cs:19-22` — подписки в конструкторе на статические события YG2. Допустимо только пока это синглтоны на всё время работы; любое пересоздание скоупа задублирует обработчики. Минимум — задокументировать в правах владения, лучше — `IDisposable`-отписка
- `Scripts/UI/Spawners/UiSpawner.cs:96-116` — `ClosedRelay` отписывается только при `Closed`; объект, уничтоженный без закрытия (выгрузка сцены), оставляет подписку живой

### 3.11 Ветвление по енуму без switch
`Scripts/Game/Level/ZoneLayoutPlanner.cs:43-58` — цепочка if/else по `SpawnShape` (4 ветки). Правило: 3+ ветки — switch.

### 3.12 «Один файл = один класс»
- `Scripts/UI/Roulette/RouletteSectorCard.cs` — struct `RouletteSectorView` + class `RouletteSectorCard` верхнего уровня → разнести по файлам
- SO + companion-классы в одном файле (8 файлов: `UpgradesConfig`+`UpgradeEntry`+`PerkEntry`, `RouletteConfig`+`RouletteSector`, `TierTable`+`TierEntry`, `SkinRarityTable`+`RaritySettings`, `PropSet`+`PropVariant`, `LocalizationTable`+`LocaleEntry`, `GridBuilder`+вложенный comparer, `ItemPropFactory`+2 вложенных). Вложенные приватные — норм; верхнеуровневые `[Serializable]`-компаньоны — по букве правила нарушение. Если это осознанный канон проекта — зафиксировать исключение в `AI_RULES.md`, чтобы не всплывало на каждом ревью.

### 3.13 Мелкое
- `Scripts/Core/Scriptables/Skins/SkinItem.cs` — публичные `[field: SerializeField]`-свойства вместо канона проекта «приватное поле + get-only свойство» (все остальные SO так)
- `Scripts/Core/Scriptables/Levels/PropSet.cs:30-32` — пустой `PropVariant()` не нужен (компилятор даёт дефолтный)
- `Scripts/UI/Roulette/RouletteView.cs:263` и `RouletteWinPopup.cs:201` — `$"×{...}"` в коде: не локализуется (спорно: символ × универсален — решить осознанно)
- `ShopLifetimeScope`/`GameLifetimeScope`/`MenuLifetimeScope`/`FillLifetimeScope` — `InjectSceneButtonSounds` скопирован 4 раза один в один → один статический хелпер (DRY)
- `Scripts/ShapeFill/FillCounter.cs` — `Game.LevelProgress` полностью квалифицирован, в остальных файлах через using — выровнять
- `Assets/MadSlime/Scenes/Shop.unity` и модуль `Scripts/UI/Shop` живут в namespace `Skins` — папка говорит `Shop`. Для новых файлов правило «namespace по папке»; текущее расхождение — осознать и либо переименовать, либо зафиксировать

### 3.14 Instantiate в логике (правило фабрик/пулов)
Каноничные спавнеры (`ItemPool`, `CubeSpawner`, `UiSpawner`, вью-фабрики) — ок. Спорные точки: `TierUpFx:88`, `FillFinale:94` (разовые VFX — терпимо), `SkinApplier:87`, `RouletteWinPopup:288`, `ModelPlacer:64` (модели скинов — одна живая, пул не нужен), `ShopPanel:344` (перkSeparator), `QuotaUI:123` (плашки квоты), `RouletteReel:200` (карточки ленты). Единый заводской паттерн для «модель скина» и «карточка ленты» закрыл бы вопрос.

### 3.15 Expression-bodied свойства — 257 шт
По букве чек-листа №16 («нет однострочных тел… свойств») каждый `public float Pull => _pull;` — нарушение. По всему проекту это устоявшийся канон, и читать его как запрет на `=> _x;` — значит обесценить правило. Требуется вердикт владельца: либо пункт №16 уточнить («братьев-однострочников с телом-блоком не писать; `=>`-геттеры разрешены»), либо проект переводится на блочные свойства. В счёт оценки не включено.

---

## 4. Платформенные правила Яндекс

### 4.1 Канвасы с Constant Pixel Size (root, Screen Space Overlay)
Правило: только `Scale With Screen Size`. Вложенные канвасы (Shop-страницы и т.п.) — норм, у них свой скейлер игнорируется. А это корневые:
- `Scenes/Menu.unity` — канвас `Background`
- `Scenes/Game.unity` — `JoystickCanvas`, `LevelLabelUI`, `GrothBarCanvas`
- `Scenes/Fill.unity` — `PauseButtonCanvas`
При этом `GameTimerCanvas`/`QuotaCanvas`/`FillProgress` — на Scale. HUD в одной сцене масштабируется по-разному: на не-референсных разрешениях элементы разъедаются относительно друг друга.
Как чинить: перевести перечисленные на `Scale With Screen Size` (портрет 1080×1920 / ландшафт 1920×1080, match 0.5) — правка сцен только через batchmode editor-скрипт.

### 4.2 Лидерборд — см. п. 1.5 (виснет на ошибке) и п. 1.6 (непереведённый заголовок).

### 4.3 Хардкод-тексты в префабах — статус
`ПОБЕДА/ПРОИГРЫШ/Заработано:` (WinMenu/FailMenu), `ВОЙТИ` (LeaderboardMenu), `Русский` (PauseMenu), `Play/Shop/DAILY/AD` (Menu) — перекрываются `LocalizedText` в рантайме, ключи в ассете есть. Это заглушки, не живой violation — но их количество превышает число `LocalizedText` в Menu.unity (6 надписей против 2 компонентов): кнопки `Settings`, часть надписей — проверить, чем именно они локализуются, ключа `menu_settings` в ассете нет.

### 4.4 Что проверено и чисто
Интерстишл — только из Fill (`NavigateToAfterStop`), кап `interAdvInterval: 60` в SettingsYG2; авторизация — только по кнопке; имя анонима — через `LBMethods.AnonymousName` (плагин); `IsPauseGame`-гварды на месте; тач-джойстик с `Touchscreen.current`, сброс драга в `OnDisable`; музыка один источник, SFX через пулы с капами; сценa Menu первая в билде; точка входа `Menu → Startup → GameDirector.EnsureInitialized` соблюдена.

---

## 5. Проверено и соответствует

- GUID-ссылки на удалённые `SpeedSmoke`, `ItemOutline.shader`, `SmokeTrail`, снесённые Editor-скрипты — чисто, ничего не висит; `QuotaHighlight.mat` указывает на живой `ItemOutlineSToon`
- Нет `var`, Reflection, Service Locator, Singleton, `Find*`, LINQ в рантайме, `async void`, анонимных лямбд в подписках
- Все события — `event Action`; нейминг `On*`-хендлеров выдержан; `Try*` возвращают `bool`
- Имена файлов = имена типов (все «несовпадения» из общего свeep — енумы/интерфейсы с корректными именами файлов)
- `.meta` на месте у всех файлов; `sealed` соблюдён почти везде; один класс-контракт сейвов (`_openSkins`, `musicVolume`, `sfxVolume`) не тронут
- Отписки `+=`/`-=` симметричны во всех gameplay-компонентах (проверял баланс по каждому файлу из списка подозреваемых — ложные срабатывания были на `?.Invoke`/`+= amount`)
- FSM-канон: `FlyingCube.State`, `ShapeFiller.BoostStage` — образцовые; idle-луп `RouletteReel` с токеном — как в правилах
- Тулзы Editor (`ItemPropFactory`, `PlayerDataResetTool`) — стильно чистые, `SerializedObject` с `Apply*`, fail-fast

## 6. Порядок правок (предлагаемый)

1. Баги: 1.1, 1.2, 1.4 (дешёвые, точечные) → 1.3 (утечка) → 1.5–1.6 (платформа)
2. Поименованные в правилах: 2.1–2.4
3. Механика: снести комментарии/тернарники/логи (3.1–3.3), `timeScale` (3.4), строку таймера (3.5)
4. Магические числа/строки, sealed, static (3.6–3.9)
5. Канвасы Constant Pixel → Scale (4.1) — batchmode-скриптом, прогнал → удалил
6. Вердикт владельца: 3.12 (SO-компаньоны), 3.15 (expression-bodied), 3.14 (VFX/модели)
