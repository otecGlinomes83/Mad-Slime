# AI_GUIDE — путеводитель по системам (для ревью)

> Слепок рабочего дерева ветки `MadSlimeV2` на 2026-09-16. Собран полным проходом по всем 123 собственным `.cs` (плагины YG2/DOTween не входят).
> Правила стиля — `AI_RULES.md`. История решений и волн — `AI_NOTES.md`. Фактическое состояние + GDD — `AI_CONTEXT.md`.
> **Как пользоваться:** выбери систему из оглавления → открой её скрипты списком → проходи файл за файлом, сверяясь с «Потоком» и «На что смотреть». Разделы самодостаточны.

## Оглавление

1. Общая картина (сцены, DI, шов Яндекса, слои)
2. Сквозные инварианты ревью (применять к любой системе)
3. DI и жизненный цикл
4. Прогресс, сейвы, экономика
5. Сессия Game
6. Платформа Яндекс (реклама / лидерборд / язык / окружение)
7. Генерация уровня
8. Предмет и квота
9. Движение и ввод
10. Детекция, сбор, тиры и рост
11. Ghost-система (рентген предметов)
12. Скиллы
13. Камера
14. Магазин и скины
15. Fill-сессия
16. UI (HUD, окна, фабрики, локализация UI)
17. Аудио
18. Editor-инструменты (конвейер пропсов, превью лейаутов)
19. Открытые хвосты, мёртвое и кандидаты на чистку
20. Ревью-панч-лист (стиль, нейминг, структура) — 2026-09-16

---

## 1. Общая картина

### Сцены (все три в билде)

| Сцена | Роль | Ключевые объекты |
|---|---|---|
| `Assets/Scenes/Game.unity` | сбор предметов, квота/таймер | ProjectScope-инстанс? нет — root-скоуп DDOL; `GameLifetimeScope`, Player (Mover/Rotator/PlayerTier/Player/CapsuleCollider/ItemGhostToggler/SkinApplier/TierUpSound), Collector+Absorber+детекторы, GameplaySessionHandler+Timer+Pauser, HUD-канвас (`UI.prefab`: GameplayUIFabric, QuotaUI, TimerUI), камера (CameraFollow+CameraImpulse), аудио (AudioMixerController, LevelMusicPlayer, PlayerPickupSound, TierUpSound, TimerTickSound, SoundLimiter, UIButtonSound), Wallet (см. §19 — подозрительно), LayoutPreviewDrawer (editor-гизмо), скиллы (AttractSkill+Timer, SkillHandler, SkillInputBinder, SkillUnlocker) |
| `Assets/Scenes/Fill.unity` | заливка формы, награда, переходы | `FillLifetimeScope`, FillSessionHandler (создаёт YandexAdsBridge в Awake), AdScheduler, LeaderboardReporter, Rewarder+Wallet, ShapeFillOrchestrator/GridBuilder/ShapeFiller/CubeSpawner, FillUIFabric (`FillUI.prefab`), FillCounter, Pauser, FlyingCubeArrivalSound, LevelTransitor |
| `Assets/Scenes/Shop.unity` | магазин | почти пуста: аудио + LevelMusicPlayer; весь магазин живёт в префабе `Assets/Resources/Prefabs/UI/Skins/Shop.prefab` (Shop, ShopPanel, ShopItemViewFactory, ModelPlacer, ShopLifetimeScope+Wallet, LevelTransitor, UIButtonSound) |

`Assets/Scenes/Test.unity` — вне билда. Переходы: Game → (квота/таймаут) → Fill → (Win: level+1) → Game; Shop — сбоку (`LevelTransitor.LoadShop` сохраняет `YG2.saves.PreviousScene`, выход — `LoadScene(PreviousScene)`).

### DI-граф (VContainer)

- **Root**: `Assets/Resources/Prefabs/DI/ProjectScope.prefab` (гуид в `Assets/Scriptables/DI/VContainerSettings.asset` → preloadedAssets, DontDestroyOnLoad). Компоненты в префабе: `ProjectLifetimeScope`, `PlayerProgress`, `LocalizationService`.
- Регистрации root: `PlayerProgress`, `LocalizationService` (компоненты); `LevelsCatalog`, `TierTable`, `LayoutsLibrary` (SO-инстансы); `LevelProgress`, `LevelConfigResolver` (plain-классы, **Singleton root-контейнера — живут между сценами**; `LevelProgress.Reset(...)` вызывается в каждом `LevelGenerator.Generate`); `SessionStateLogger` (entry point, `IStartable`, plain-класс).
- Сценовые скоупы без явного родителя цепляются к root → видят его регистрации: `GameLifetimeScope` (компонент в Game.unity; регистрирует PlayerConfig, ItemPool, QuotaGenerator, LevelGenerator, Player, PlayerTier, GameplaySessionHandler, QuotaUI, CameraImpulse, SkinApplier, LevelLabelUI, SkillUnlocker), `FillLifetimeScope` (FillSessionHandler, FillCounter, FillUIFabric, Wallet), `ShopLifetimeScope` (компонент внутри Shop.prefab; только Wallet).
- Компоненты, потребляющие `[Inject]`, но НЕ зарегистрированные скоупом, инъекцию не получают (`_progress == null`): в Game-сцене так живёт `Wallet` — зарегистрирован только в Fill/Shop. Смотри §19.

### Шов платформы

- Сейвы: `SavesYG` (partial в `Assets/Scripts/Saves/Saves.cs`, поля = JSON-ключи, **не переименовывать**) ← единственное окно — `PlayerProgress` (+ `Save()` с guard `YG2.isSDKEnabled`). Асимметрия: `ShopPanel` и `LevelTransitor` пишут `YG2.saves` напрямую.
- Реклама/лидерборд/язык/PlayerId — собственные jslib-мосты (`Assets/Plugins/MadSlimeYandex.jslib`), т.к. в YG2 v2.0092 модулей Adv/Leaderboard/Localization нет. Приёмники: `YandexAdsBridge` (GO «YandexAdsBridge», создаётся кодом), `YandexEnvironmentBridge` (GO «YandexLangBridge», ребёнок LocalizationService).

### Слои (TagManager)

`Collectable = 3` (все коллайдеры предметов), `Player = 6`, `SkinsRender = 7`, `Wall = 8` (борта). Маска `MoveChecker` = Collectable|Wall; маски детекторов и `ItemGhostToggler` = Collectable.

---

## 2. Сквозные инварианты ревью (применять к любой системе)

1. **Fail-fast — политика владельца:** невалидная serialized-ссылка → `InvalidOperationException` в `Awake`/`Construct` с текстом «Drag … into the _field field»; плохой аргумент → `ArgumentOutOfRangeException`. Молчаливые early-return на невалиде запрещены. **Известные нарушения по текущему коду** (готовые цели ревью): `Player.Awake` (молчаливый return при null `PlayerConfig`; не валидирует `_inputReader`/`_collector`), `GenericOverlapDetector` (молчаливое отключение tier-подписки при null-источниках), `SkinApplier.ApplySelectedSkin` (return при null `_progress`), `SkillUnlocker.IsUnlocked` (false при null `_progress`), `SkillHandler`, `SkillInputBinder`, `MassUI`, `LevelRewardPopup`, `PauseMenu` (`_settingsPanel`), `ShopItemViewFactory` (префаб без валидации), `SessionStateLogger` (валидаций нет вовсе).
2. **Подписки:** только пары `OnEnable`/`OnDisable`, только именованные методы, никаких лямбд в `+=`. Гарды двойной подписки флагом есть в `CameraImpulse`, `GameplaySessionHandler`, `QuotaUI` (`_isSubscribed`) — паттерн под VContainer-инжект, приходящий после OnEnable. Асимметрия Awake/OnDisable — баг-паттерн: `LocalizationService` подписывает bridge-события в `Awake`, отписывает в `OnDisable` (после re-enable ответы Яндекса уходят в никуда).
3. **Пауза:** единственный писатель `Time.timeScale` — `Pauser` (счётчик запросов; resume гейтится `YG2.isPauseGame`). Явные timeScale-гейты в Update: `GenericOverlapDetector`, `ItemGhostToggler`; в цикле таймера — `Timer`. Замирают по `deltaTime = 0`: Mover/Rotator, CameraFollow, CameraImpulse, SkinApplier-не, Absorber и LevelScaler (крутят пустые циклы каждый кадр). НЕ замирает: `PlayerInputReader` (ввод работает на паузе → скилл можно активировать), `ModelPlacer` (вращение на `unscaledDeltaTime`).
4. **Базы снимаются в `Awake`** — компаундинга при пере-входе нет: `GenericOverlapDetector._baseRadius`, `LevelScaler._baseController*`, `CameraFollow._startOffset`, `Item._defaultScale/_originalMaterials` (снапшот sharedMaterials).
5. **Пул предметов:** `LevelGenerator.SpawnItems` → `ItemPool.Get` (LIFO под GO «PooledItems») → `Item.SetDefinition` → `Item.Initialize` → … → `Collector` → `Item.Collect` → `Absorber` → `ItemCollected` → `ItemPool.Release` → `Item.Shutdown`. Правило: любое новое состояние `Item` сбрасывать в `Initialize`.
6. **Сейвы:** поля `SavesYG` — JSON-ключи живых сейвов (`_openSkins` с underscore — так задумано). `Save()` только через `PlayerProgress` (guard SDK); исключения — ShopPanel/LevelTransitor напрямую. `AudioMixerController` дебаунсит `SaveProgress` на 500 мс.
7. **`[Diag]`-логи** (волны 22–25, отладка «всё притягивается»): `PlayerTier`, `GenericOverlapDetector`, `LevelScaler`, `SkinApplier`, `ItemGhostToggler`, `Collector`, `SessionStateLogger` (+timeScale). Грепаются по `[Diag]`. Держать до подтверждения владельца, потом снести — это аллокации строк в горячих путях.
8. **Рандом — два несведённых источника:** `UnityEngine.Random` (layout/тиры/квота/зеркала в `LevelGenerator`, `QuotaGenerator`) и несидированный `System.Random` (позиции в `ZoneLayoutPlanner`). Воспроизводимая генерация уровня невозможна — это факт дизайна, не баг.
9. **Стиль:** чек-лист `AI_RULES.md` (17 пунктов: нет var/комментариев/лямбд-подписок, `== false`, braces на новой строке, sealed, `[SerializeField] private`, …). Замеченная грязь: `UIButtonSound._buttons` (`[SerializeField]private  List<Button>`), `ShopContent` OnValidate-сообщение без пробела, `IMassHolder` явный `public` на члене интерфейса.
10. **Компиляция без редактора:** `~/Unity/Hub/Editor/2022.3.62f2/Editor/Unity -batchmode -quit -nographics -projectPath . -logFile /tmp/u.log` (только при отсутствии `Temp/UnityLockfile`). Волны 20+ AI_NOTES batchmode не прогонялись (редактор был открыт).

---

## 3. DI и жизненный цикл

**Ответственность:** сборка графа зависимостей; root-состояние между сценами (прогресс, резолвер конфигов, LevelProgress).

| Файл | Класс | Роль |
|---|---|---|
| `Assets/Scripts/DI/ProjectLifetimeScope.cs` | `ProjectLifetimeScope : LifetimeScope` | root: валидирует 5 полей, регистрирует PlayerProgress/LocalizationService/LevelsCatalog/TierTable/LayoutsLibrary + Singleton LevelProgress, LevelConfigResolver + entry point SessionStateLogger |
| `Assets/Scripts/DI/GameLifetimeScope.cs` | `GameLifetimeScope : LifetimeScope` | Game-сцена: валидация 10 полей, `RegisterInstance(PlayerConfig)`, `Register<ItemPool/QuotaGenerator>(Scoped)`, `RegisterComponent` для 8 компонентов сцены |
| `Assets/Scripts/DI/FillLifetimeScope.cs` | `FillLifetimeScope : LifetimeScope` | Fill-сцена: FillSessionHandler, FillCounter, FillUIFabric, Wallet |
| `Assets/Scripts/DI/ShopLifetimeScope.cs` | `ShopLifetimeScope : LifetimeScope` | живёт внутри Shop.prefab; только Wallet |

**Поток:** preloadedAssets → root создаётся до сцен, DDOL → сценовый скоуп строит свой контейнер с parent=root → `[Inject] Construct(...)` у сценовых компонентов (регистрация обязательна — `RegisterComponent`) → `SessionStateLogger.Start()` (IStartable) логирует прогресс и подписывается на `SceneManager.sceneLoaded`.

**На что смотреть:**
- Валидация в `Configure` — эталон fail-fast (тексты «Drag … into the … field»).
- `LevelProgress`/`LevelConfigResolver` — Singleton root'а: переживают смену сцен; anyone, кто держит ссылку, видит состояние ТЕКУЩЕГО уровня только после `LevelProgress.Reset`.
- `SessionStateLogger` подписан на статический `SceneManager.sceneLoaded` и никогда не отписывается (по дизайну — он вечный); в логе квоты от «своего» LevelProgress.
- `Wallet` есть компонентом в Game.unity, но не зарегистрирован в GameLifetimeScope → в Game-сцене `[Inject]` не выполнится (см. §19).

---

## 4. Прогресс, сейвы, экономика

**Ответственность:** единственный шов над `YG2.saves`, деньги, расчёт награды.

| Файл | Класс | Роль |
|---|---|---|
| `Assets/Scripts/Saves/Saves.cs` | `SavesYG` (partial, ns YG) | поля-JSON-ключи: `CurrentLevel, MaxLevel, Language, PlayerId, musicVolume, sfxVolume, Balance, SelectedSkinType, _openSkins, PreviousScene` |
| `Assets/Scripts/Game/PlayerProgress.cs` | `PlayerProgress : MonoBehaviour` | property-шов над каждым полем сейва; `Awake`: Guest-id fallback (`Guest-XXXXXX`, константа `GuestIdPrefix`); `Save()` — guard `isSDKEnabled` → `YG2.SaveProgress()` |
| `Assets/Scripts/Game/Wallet.cs` | `Wallet : MonoBehaviour` | `Add/Spend` (строгие: `ArgumentOutOfRangeException` при ≤0, `InvalidOperationException` при нехватке), `event Action<int,int> BalanceChanged(prev,new)`, сейв на каждую операцию |
| `Assets/Scripts/Game/Rewarder.cs` | `Rewarder : MonoBehaviour` | `RewardWin(percent≥1)`: base, при percent > 1.25 → `Round(BaseReward*percent)`; `RewardLose`: percent ≥ 0.25 → `BaseReward / LoseRewardDivisor`, иначе 0; событие `RewardGranted(amount,isWin)` инвоукается ДО `Wallet.Add` |
| `Assets/Scripts/Scriptables/Rewards/RewardConfig.cs` | `RewardConfig : ScriptableObject` | `BaseReward=50`, `WinFullMultiplierThreshold=1.25`, `LoseMultiplierThreshold=0.25`, `LoseRewardDivisor=4` |

**Поток:** FillSessionHandler.OnFillCompleted → Rewarder → `RewardGranted` → FillSessionHandler публикует `Win/Failed` (окна) → Wallet.Add → `BalanceChanged` → сейв.

**На что смотреть:**
- Порядок: `RewardGranted` раньше зачисления → окна получают сумму раньше, чем она появится в кошельке (WinMenu «удвоить» считает от `_lastRewardAmount`).
- `RewardConfig.LoseRewardDivisor = 0` в ассете → DivideByZeroException; `_baseReward ≤ 0` стрельнет только в `Wallet.Add` (ассет не валидируется).
- `Wallet` — нет проверки `_progress == null` в Awake (NRE вместо исключения).
- Схема сейва без версионирования/миграций — partial чужого плагина.

---

## 5. Сессия Game

**Ответственность:** старт по первому вводу, таймер, завершение по квоте/таймауту, переход в Fill, счётчик паузы, переходы между сценами.

| Файл | Класс | Роль |
|---|---|---|
| `Assets/Scripts/Game/GameplaySessionHandler.cs` | `GameplaySessionHandler : MonoBehaviour` | Awake: fail-fast на инъекции, `Timer.Setup(config.TimerDuration)`, `Pauser.RequestPause()` (сцена рождается на timeScale=0); старт по `_inputReader.MovementKeyPressed` → resume → `Timer.StartCount()` → `YG2.GameplayStart()` → event `GameStarted`; финиш: `Timer.Finished` / `LevelProgress.QuotaCompleted` → `YG2.GameplayStop()` → `LevelTransitor.LoadFill()`; `Restart()` |
| `Assets/Scripts/Game/Pauser.cs` | `Pauser` | счётчик `_pauseRequestCount`; resume ставит timeScale=1 только если `YG2.isPauseGame == false` |
| `Assets/Scripts/Game/Timer.cs` | `Timer : MonoBehaviour` | UniTask-цикл (`PlayerLoopTiming.Update` + linked CTS), гейт `Time.timeScale > 0` внутри цикла; события `Ticked(float)`, `Finished`; `Setup/StartCount/Stop/Continue` |
| `Assets/Scripts/Game/LevelTransitor.cs` | `LevelTransitor : MonoBehaviour` | `Restart/LoadGame/LoadFill/LoadShop/LoadScene`; `LoadShop` пишет `YG2.saves.PreviousScene` + сейв; сцены грузятся синхронно (single) |
| `Assets/Scripts/Game/SessionStateLogger.cs` | `SessionStateLogger : IStartable` | диаг-лог прогресса на старте и по `sceneLoaded` (+timeScale, волна 22) |

**На что смотреть:**
- `GameplaySessionHandler.OnDisable` отписывается без null-проверок serialized-полей; `Awake` валидирует только инъекции (`_inputReader/_timer/_pauser/_levelTransitor` — нет).
- `Timer.StartCount` на исчерпанном таймере → мгновенный повторный `Finished` (`Continue` защищён, `StartCount` — нет).
- `LevelTransitor` не сбрасывает timeScale: Game сам паузит себя в Awake, Fill стартует с «протёкшим» значением (норма, но знать).
- Избыточный `RequestPause` без парного resume навсегда держит паузу (счётчик).
- Двойная страховка инъекции: OnEnable — тихий early-return, Start — исключение (осознанно).

---

## 6. Платформа Яндекс

**Ответственность:** реклама, лидерборд, язык, PlayerId — всё через собственные jslib-мосты (в YG2 v2.0092 этих модулей нет; defines `RewardedAdv_yg` и пр. включать НЕЛЬЗЯ — упадёт компиляция).

| Файл | Класс | Роль |
|---|---|---|
| `Assets/Scripts/Game/Ads/YandexAdsBridge.cs` | `YandexAdsBridge : MonoBehaviour` | статический `Create()` (GO по имени `ReceiverName = "YandexAdsBridge"`, НЕ DDOL); DllImport в jslib (`RewardedAdvShowMadSlime_js`, `InterstitialAdvShowMadSlime_js`, `SetLeaderboardScoreMadSlime_js`, `GetLeaderboardEntriesMadSlime_js`) только под `UNITY_WEBGL && !UNITY_EDITOR`, иначе симуляция; 10+ событий; приватные `On*`-приёмники для SendMessage из JS; синхронизирует `YG2.nowRewardAdv/nowInterAdv`; DTO `LeaderboardPayload/LeaderboardEntry` |
| `Assets/Scripts/Game/AdScheduler.cs` | `AdScheduler : MonoBehaviour` | `Setup(bridge)`; `ShowInterstitialIfNeeded(level)` (делитель `YandexConfig.InterstitialEveryLevels`, гейт `YG2.nowAdsShow`); `ShowDoubleReward/ShowRewarded(id, granted[, rejected])` — награда только при пришедшем `OnRewardedReceived` (флаг `_rewardedReceived`); pending-экшены `_pendingRewardAction/_pendingRejectedAction` |
| `Assets/Scripts/Game/LeaderboardReporter.cs` | `LeaderboardReporter : MonoBehaviour` | `Setup(bridge)`, `Report(MaxLevel, PlayerId)` → `SetLeaderboardScore` (extraParam = PlayerId — виден в рейтинге для анонимов); fail-fast на имя лидерборда |
| `Assets/Scripts/Game/Localization/YandexEnvironmentBridge.cs` | `YandexEnvironmentBridge : MonoBehaviour` | GO «YandexLangBridge», ребёнок LocalizationService (создаётся кодом в его Awake); `RequestLanguage()/RequestPlayerId()`; события `LangReceived/PlayerIdReceived`; в редакторе отвечает `string.Empty` |
| `Assets/Scripts/Game/Localization/Localization.cs` | `static class Localization` | фасад: `Initialize(table, savedLang)`, `Get(key)`, `SetLanguage/CycleLanguage` (ru→en→tr), `event Action LanguageChanged`; null/empty → системный язык, неизвестный → en |
| `Assets/Scripts/Game/Localization/LocalizationService.cs` | `LocalizationService : MonoBehaviour` | на ProjectScope; `Awake`: создаёт environment-бридж, `Localization.Initialize(_table, YG2.saves.Language)`, подписки; язык из Яндекса применяет ТОЛЬКО если сейв пуст (авто-режим); PlayerId из SDK перезаписывает Guest-; смену языка персистит |
| `Assets/Scripts/Scriptables/Localization/LocalizationTable.cs` | `LocalizationTable : ScriptableObject` (+ `LocaleEntry`) | key → ru/en/tr; фолбэки en↔ru, tr→en; нет ключа → возвращается сам ключ |
| `Assets/Scripts/Scriptables/Ads/YandexConfig.cs` | `YandexConfig : ScriptableObject` | `InterstitialEveryLevels` (Min 1, кламп в свойстве), `DoubleRewardId="DoubleReward"`, `NextLevelRewardId="NextLevel"`, `LeaderboardName="max_level"` |
| `Assets/Plugins/MadSlimeYandex.jslib` | — | js-функции; зовут глобальный `ysdk` (ставит бутстрап плагина); колбэки через `SendMessage('YandexAdsBridge'/'YandexLangBridge', ...)` |

**Поток рекламы:** FillSessionHandler.Awake → `YandexAdsBridge.Create()` → `Setup` обоим (AdScheduler, LeaderboardReporter) → кнопки окон вызывают AdScheduler → jslib → ysdk → SendMessage-колбэки → события бриджа → pending-экшены AdScheduler.

**На что смотреть:**
- **Хрупкий контракт имён** с jslib (GO + имена методов-приёмников с двух сторон): расхождение = молчаливое отсутствие колбэков, pending-действия зависнут.
- `AdScheduler`: нет защиты от повторного `ShowRewarded` до закрытия первого — pending-экшены перетираются; `Awake`-проверка `InterstitialEveryLevels <= 0` мертва (кламп в `YandexConfig`).
- Бридж не DDOL: ads-бридж живёт в Fill-сцене; `LeaderboardMenu` создаёт свой собственный экземпляр (Game-сцена) и убивает в OnDisable.
- `LocalizationService`: подписки на bridge в Awake, отписка в OnDisable — после re-enable `RequestLanguage/RequestPlayerId` отвечают в никуда.
- Редакторная симуляция: rewarded — сразу granted; interstitial — только Closed; entries — синтетика (rank 1, score 100).
- Владельцу (не код): лидерборд `max_level` должен существовать в Яндекс.Консоли.

---

## 7. Генерация уровня

**Ответственность:** номер уровня → конфиг → случайный layout → назначение тиров пропсам → раскладка точек → спавн из пула → квота из заспавненного. Тема (материал пола, текстура формы).

| Файл | Класс | Роль |
|---|---|---|
| `Assets/Scripts/Scriptables/Levels/LevelsCatalog.cs` | SO | `Ranges: List<LevelRange>` |
| `Assets/Scripts/Scriptables/Levels/LevelRange.cs` | plain [Serializable] | `FromLevel/ToLevel/Config` |
| `Assets/Scripts/Scriptables/Levels/LevelConfig.cs` | SO | `Theme, PropSet, MinTier/MaxTier (кламп), TimerDuration=90`, блок Quota (`QuotaTypesMin/Max`, `QuotaTargetMin/Max`, `QuotaMaxSameTier`, `DefaultCountDivisor=4`) |
| `Assets/Scripts/Game/Level/LevelConfigResolver.cs` | plain (DI Singleton) | `GetConfigFor(level)`: первый matching range; за пределами — последний; не-playable (нет PropSet/вариантов) → LogWarning + первый playable; ничего → `InvalidOperationException` |
| `Assets/Scripts/Scriptables/Levels/LayoutsLibrary.cs` | SO | общий пул `List<LayoutSet>` для всех локаций |
| `Assets/Scripts/Scriptables/Levels/LayoutSet.cs` | SO | `Zones`, `AllowMirroring`, `AutoSpacingFactor=2.2`, `ScatterDistanceFactor=0.7` |
| `Assets/Scripts/Scriptables/Levels/SpawnZone.cs` | plain | `Shape/Center/Radius/Count/AutoSpacing/Spacing/MinTier/MaxTier` |
| `Assets/Scripts/Scriptables/Levels/SpawnShape.cs` | enum | `Grid, Circle, Scatter, CircleGrid` |
| `Assets/Scripts/Scriptables/Levels/PropSet.cs` | SO (+ `PropVariant`) | `_props` (юнит-префабы), `_variants` (префаб+definition, продукт Prop Factory) |
| `Assets/Scripts/Scriptables/Levels/LevelTheme.cs` | SO | `FloorMaterial`, `FillShapeTexture` |
| `Assets/Scripts/Game/Level/ZoneLayoutPlanner.cs` | plain sealed | чистая математика 4 форм; `ResolveSpacing` (ручной spacing при `AutoSpacing==false`, иначе `Max(0.5, maxRadius*factor)`); `Collect` пишет в переиспользуемый `_positions` |
| `Assets/Scripts/Game/Level/LevelGenerator.cs` | MonoBehaviour (DI) | оркестратор: `Awake → Generate()`; подписан на `Collector.ItemCollected` → `ItemPool.Release`; `MapSize`; `ApplyTheme` (мягкая) |
| `Assets/Scripts/Game/Level/ItemPool.cs` | plain (DI Scoped) | пул по префабам под GO «PooledItems», LIFO; `Release` вызывает `Shutdown` |
| `Assets/Scripts/Game/Level/ItemSize.cs` | static | XZ-радиус префаба по BoxCollider (fallback 1) |

**Поток (`LevelGenerator.Generate`):**
1. `GetConfigFor(PlayerProgress.CurrentLevel)` (fallback-семантика «контент не готов» — волна 10).
2. `PickLayout`: playable = не-null и есть зоны; случайный (Unity Random); ничего → throw.
3. `ApplyTheme`: `floorRenderer.sharedMaterial = config.Theme.FloorMaterial` (мягкий null-check).
4. `AssignTiers`: варианты PropSet по тиру в диапазоне конфига → префабы шаффлятся → каждому префабу ОДИН definition на уровень (точный тир для первых, иначе случайный; `FindByTier` при отсутствии точного тихо берёт `options[0]`).
5. `SpawnItems`: зеркала (`AllowMirroring && Random.value > 0.5` по каждой оси) → на зону: пул вариантов по `zone.MinTier..MaxTier`, радиусы `ItemSize * TierTable.Scale`, `ResolveSpacing`, `ZoneLayoutPlanner.Collect` (Grid: rows=sqrt; CircleGrid: кольца; Circle: по окружности; Scatter: rejection sampling `Radius*sqrt(u)`, min-dist `spacing*ScatterDistanceFactor`, лимит попыток count*10) → `ItemPool.Get` → `SetDefinition` → `Initialize(ClampToMap(pos), TierTable.Scale)`.
6. `Physics.SyncTransforms()` (волна 25 — фикс stale-физики после телепорта из пула).
7. `QuotaGenerator.Generate(spawnedCounts, config)` → `LevelProgress.Reset(quota, divisor)`.

**На что смотреть:**
- **Снежный ком данных (волна 19):** TableLayoutSet — Scatter radius 100 / count 1000 при `_mapSize` 30×30 → clamp сплющивает позиции на борта. Это данные, не код; кандидат-гард: warning «N из M за картой».
- Тихие ветки: недобор Scatter-позиций без warning; зона без пропсов — только warning; `QuotaGenerator` при typesTarget=0 отдаёт пустую квоту → `FillPercent` навсегда 0 (уровень непроходим молча).
- SO-ассеты не валидируют себя (`LevelRange` инвертированный, `QuotaTypesMin > Max`, коэффициенты LayoutSet ≤ 0) — всё ловится клампами/тихо.
- Дублирование случайностей: превью детерминировано (seed `layoutIndex*7919 + zoneIndex*17 + 3`), рантайм — нет; «превью ≠ уровень» осознанно.
- `ItemPool.Get`: деактивированный префаб-ассет → только warning, предмет невидим.

---

## 8. Предмет и квота

**Ответственность:** пулимый предмет (данные + визуал + ghost), запись квоты, модель прогресса уровня.

| Файл | Класс | Роль |
|---|---|---|
| `Assets/Scripts/Item/Item.cs` | `Item : MonoBehaviour, IAttractable` (DOTween) | `Definition/Mass/Tier` (throw при null-definition — волна 11), `SetDefinition`, `Initialize(pos, scale)`, `Collect`, `Shutdown`, ghost-механика `SetGhost/ResetVisuals` (детали §11) |
| `Assets/Scripts/Scriptables/Items/ItemDefinition.cs` | SO | `Icon, BaseMass, Tier` — идентичность квоты по ССЫЛКЕ на ассет (два ассета с одинаковым контентом = разные типы) |
| `Assets/Scripts/Scriptables/Items/GhostFadeConfig.cs` | SO | `FadeDuration=0.25`, `Ease` (DG.Tweening) — ассет `Assets/Scriptables/Items/GhostFadeConfig.asset` |
| `Assets/Scripts/Quota/QuotaEntry.cs` | plain [Serializable] | `Definition/TargetCount/Collected/Remaining`; ctor fail-fast |
| `Assets/Scripts/Game/Level/LevelProgress.cs` | plain sealed (DI Singleton) | `Reset(quota, divisor)`, `RegisterCollected(definition)`; события `ItemCollected(def)`, `QuotaChanged(remaining, entry)`, `QuotaCompleted` (однократно); `FillPercent = Clamp01((quota + default/divisor) / total)` |
| `Assets/Scripts/Game/Level/QuotaGenerator.cs` | plain (DI Scoped) | из заспавненных типов: кандидаты со спавном ≥ QuotaTargetMin; shuffle; лимит на тир; target > spawned → warning + срез |
| `Assets/Scripts/Interfaces/IAttractable.cs` / `IMassHolder.cs` | интерфейсы | `Tier/Self/Mass`; реализует `Item` |

**На что смотреть:**
- `Item.Awake` fail-fast: коллайдер, ghost-материал (+`HasProperty(_Opacity)`), ghost-fade-конфиг (duration > 0), наличие рендереров. Все Item-префабы ОБЯЗАНЫ иметь `_ghostMaterial` и `_ghostFadeConfig` — иначе падение на спавне каждого предмета (было в волне 23).
- Материальный снапшот `_originalMaterials` один раз в Awake — переживает переиспользование из пула; любое изменение материалов после Awake протечёт.
- `LevelProgress.RegisterCollected(null)` — единственный тихий early-return кластера.
- `FillPercent` при пустой квоте (total=0) → 0 навсегда — следствие QuotaGenerator (§7).
- Порядок событий: `ItemCollected(def)` инвоукается до инкремента счётчиков.

---

## 9. Движение и ввод

**Ответственность:** ввод → сглаженное движение/поворот через гейт SphereCast; конфиг чисел игрока.

| Файл | Класс | Роль |
|---|---|---|
| `Assets/Scripts/PlayerInput/PlayerInputReader.cs` | `PlayerInputReader : MonoBehaviour` | обёртка над автогеном `PlayerInputActions` (автоген не править руками; `.inputactions`-мутант в `Assets/Settings/Input/` — не рабочий файл); `MoveInput`; события `MovementKeyPressed` (переход 0→non-zero) и `AttractPerformed`; `Dispose()` ассета в OnDestroy |
| `Assets/Scripts/Player/Player.cs` | `Player : MonoBehaviour` (RequireComponent Mover/Rotator/PlayerTier) | Update: `ConvertToWorldDirection` (мировые оси, не камерно-относительные) → `Mover.Move` + `Rotator.Rotate`; `OnItemCollected` → `LevelProgress.RegisterCollected` + `PlayerTier.Add(item.Mass)`; `[Inject] Construct(LevelProgress, PlayerConfig)` |
| `Assets/Scripts/Movement/Mover.cs` | `Mover` (RequireComponent MoveChecker) | SmoothDamp-velocity, `MoveChecker.IsAbleToMove` → запрет обнуляет velocity (разгон с нуля после стены), `transform.position +=` (физика в обход) |
| `Assets/Scripts/Movement/MoveChecker.cs` | `MoveChecker` | `SphereCast(pos, collider.radius, dir, dist, _layerMask)`; hit: `IAttractable` → пропустить (сквозь ЛЮБОЙ предмет проходим — волна 18), не-attractable (стены, слой Wall) → блок |
| `Assets/Scripts/Movement/Rotator.cs` | `Rotator` | `LookRotation` + `RotateTowards` |
| `Assets/Scripts/Scriptables/Player/PlayerConfig.cs` | SO | `BaseMoveSpeed=4, RotationSpeed=420, MoveSmoothTime=0.12, MassPickupDivisor=4, AbsorptionDuration=0.3` — центральный тюнинг игрока (волна 4) |

**История стен (важно при ревью MoveChecker):** волна 2 — слой Wall(8), борта переведены, маска 264 (Collectable|Wall); волна 16–18 — владелец снял блок по тиру: предметы (IAttractable) ВСЕ проходимы насквозь, блок только не-attractable (стены); поле `_playerTier` из MoveChecker удалено (старая ссылка в сцене игнорируется). Ghost-сетка (§11) — отдельная визуальная история.

**На что смотреть:**
- `Player.Awake`: молчаливый return при null `PlayerConfig` + нет валидации `_inputReader/_collector` — нарушение канона (см. §2.1).
- `PlayerConfig.BaseMoveSpeed` фактически живёт до первого `LevelScaler.OnEnable` — дальше скорость всегда тировая (`TierThreshold.Speed`).
- `MoveChecker` кастует ЛОКАЛЬНЫМ `collider.radius` без lossyScale (при scale=1 и росте через LevelScaler — сходится; при неравномерном скейле разойдётся).
- Маска не валидируется: пустая маска = игрок проходит всё.
- Ввод работает на паузе (`MovementKeyPressed`/`AttractPerformed` приходят при timeScale=0).

---

## 10. Детекция, сбор, тиры и рост

**Ответственность:** OverlapSphere-детект → фильтр тира → всасывание → масса/тир → масштаб модели/коллайдера/скорости/радиусов.

| Файл | Класс | Роль |
|---|---|---|
| `Assets/Scripts/Detection/GenericOverlapDetector.cs` | `abstract GenericOverlapDetector<T>` | Update (гейт timeScale): `OverlapSphereNonAlloc(buffer[256])` → `TryGetComponent<T>` → `Detected(T)` КАЖДЫЙ кадр без дедупликации; `_baseRadius` в Awake; `_radius = base * TierResolver.GetScaleFor(tier)` по `TierChanged`; `SetRadius` |
| `Assets/Scripts/Collectables/ItemDetector.cs` | `: GenericOverlapDetector<Item>` | пустой наследник — сбор |
| `Assets/Scripts/Collectables/AttractableDetector.cs` | `: GenericOverlapDetector<IAttractable>` | пустой наследник — притяжка (СВОЙ радиус) |
| `Assets/Scripts/Collectables/Collector.cs` | `Collector` | фильтр `item.Definition.Tier > CurrentTier → skip` (равный собирается); `UniTaskVoid`: `item.Collect()` ДО анимации (коллайдер off → дедупликация) → `Absorber.AbsorbAsync` → `Shutdown` → event `ItemCollected(Item)` |
| `Assets/Scripts/Collectables/Absorber.cs` | `Absorber` | UniTask-лерп позиции к себе + scale → 0 (SmoothStep, `PlayerConfig.AbsorptionDuration`, serialized-ссылка на ассет) |
| `Assets/Scripts/Player/PlayerTier.cs` | `PlayerTier` | `Add(amount)`: `Max(1, Round(amount / MassPickupDivisor))`; `MassChanged`, `TierChanged` (инвоукается ВСЕГДА, даже без смены — подписчики фильтруют сами) |
| `Assets/Scripts/Player/TierResolver.cs` | `TierResolver` | масса→тир (сортировка по RequiredMass), `GetSpeedFor/GetScaleFor/GetTierProgress/GetCameraOffsetFor` из `TierScalerConfig` |
| `Assets/Scripts/Player/LevelScaler.cs` | `LevelScaler` | UniTask SmoothDamp multiplier; модель — каждый кадр, коллайдер/корень — квантованно (шаг 1%); `Mover.SetDefaultSpeed(tier)`; базы коллайдера в Awake |
| `Assets/Scripts/Scriptables/Tier/TierScalerConfig.cs` / `TierThreshold.cs` | SO / plain | пороги: `Tier, RequiredMass, ScaleMultiplier, Speed, CameraOffsetMultiplier` |
| `Assets/Scripts/Scriptables/Tiers/TierTable.cs` | SO (+`TierEntry`) | отображение тира: `Scale, Mass, BadgeColor, ShortLabel`; `Get(tier)` — throw без записи |
| `Assets/Scripts/Skills/ItemTier.cs` | enum | `Small, Medium, Large, Boss` — ось всех сравнений `>`/`<=` |

**Поток:** `ItemDetector.Detected` (каждый кадр) → `Collector.OnItemDetected` (гейт тира) → `Item.Collect()` → `Absorber` (длительность из PlayerConfig) → `ItemCollected` → `Player.OnItemCollected` → `LevelProgress.RegisterCollected` + `PlayerTier.Add` → `TierChanged` → LevelScaler (модель/коллайдер/скорость) + GenericOverlapDetector (радиусы) + CameraFollow (офсет) + CameraImpulse (толчок) + HUD.

**На что смотреть:**
- Отклонения от fail-fast: `GenericOverlapDetector` (молчаливое отключение tier-масштабирования), `Player.Awake` (§9).
- Компаундинг радиуса закрыт (baseRadius в Awake — волна 24); проверять новые подписчики `TierChanged` на фильтр равных тиров.
- `Collector.CollectAsync` при уничтожении игрока посреди анимации: предмет остаётся без коллайдера и НЕ возвращается в пул (catch → return без `Shutdown`).
- `TierChanged` без смены тира — рассылка всегда; безобидно для текущих подписчиков, опасно для будущих с побочными эффектами.
- Старт с тиром выше Small: `CameraFollow` компенсирует (офсет в OnEnable), `LevelScaler` — НЕТ (масштаб 1x до первой реальной смены тира).
- `TierThreshold._speed = 1` (дефолт забытой строки) пройдёт все валидации как «легальная» скорость 1.
- Буфер детекторов 256 — переполнение молча теряет хвост.

---

## 11. Ghost-система (рентген предметов)

**Ответственность:** предметы тиром ВЫШЕ игрока рисуются «сеточкой» (screen-door dither), пока игрок рядом. Игрок только командует — свап материалов живёт внутри Item (волна 17).

| Файл | Роль |
|---|---|
| `Assets/Scripts/Collectables/ItemGhostToggler.cs` | поллинг на Player: Update (гейт timeScale) `OverlapSphereNonAlloc(buffer[64])`, радиус = `collider.radius * |lossyScale.x| + _margin` (волна 25 — учёт роста капсулы); свип `_ghostItems` (вышел → `SetGhost(false)`) → гейт `item.Tier <= CurrentTier → skip` → новые → `SetGhost(true)` |
| `Assets/Scripts/Item/Item.cs` (ghost-часть) | `SetGhost(true)`: свап `sharedMaterials` на `MadSlime/GhostDither` на «сплошном» кадре → DOTween `_Opacity` от 1.0 к target (читается из материала в Awake) через `MaterialPropertyBlock`; `SetGhost(false)`: твин к 1.0 → `OnFadeCompleted` → возврат оригиналов + `SetPropertyBlock(null)`; `DOTween.Kill(this)` перед каждым (SetTarget обязателен); `SetLink(gameObject, KillOnDisable)`; `ResetVisuals` (жёсткий сброс) в `Initialize` и `Collect` |
| `Assets/Shaders/GhostDither.shader` | opaque-проход, screen-space дизер 4×4 Bayer + `clip(_Opacity - threshold)`, half-lambert; ghost не кастит тень (осознанно) |
| `Assets/Scriptables/Items/GhostFadeConfig.asset` | duration 0.25, ease InOutSine — тюнинг только тут; плотность сетки — `_Opacity` в `Assets/Shaders/GhostMaterial.mat` |

**На что смотреть:**
- Волны 16–21, 25 — полная история (идея → шейдер → плавный фейд → конфиг → lossyScale).
- Провода владельца: GhostMaterial.mat + GhostFadeConfig.asset обязаны лежать в КАЖДОМ Item-префабе (Prop Factory пишет) — без них fail-fast в Awake (волна 23).
- `_ghostItems` — O(ghosts × hits) в кадр (Contains + TryGetComponent); буфер 64 < 256.
- Ghost и блок прохода независимы: проход определяется только `MoveChecker` (все attractable проходимы насквозь).
- `[Diag]`-лог на каждый toggle — шумный.

---

## 12. Скиллы

**Ответственность:** активная притяжка: ввод → разблокировка по уровню → FSM активная фаза/кулдаун на общем Timer.

| Файл | Класс | Роль |
|---|---|---|
| `Assets/Scripts/Skills/BaseSkill.cs` | `abstract BaseSkill` | FSM на `Game.Timer`: `TryActivate` (занят → false; `Duration <= 0` → мгновенный синхронный цикл); хуки `OnActivated/OnTick/OnDeactivated`; события `Started/Ended/CooldownEnded` |
| `Assets/Scripts/Skills/AttractSkill.cs` | `AttractSkill : BaseSkill` | подписка `AttractableDetector.Detected`; гейты: `IsActive`, `Tier > CurrentTier → skip`; кривая скорости: `multiplier = 1 + (ApproachMultiplier-1) * (1 - d/R)^ApproachPower` (волна 14); transform-сдвиг без физики/clamp |
| `Assets/Scripts/Skills/SkillHandler.cs` | `SkillHandler` | реестр `List<BaseSkill>`, `TryActivate(config)` по ссылочному сравнению |
| `Assets/Scripts/Skills/SkillInputBinder.cs` | `SkillInputBinder` | `AttractPerformed` → `SkillUnlocker.IsUnlocked` → `SkillHandler.TryActivate` |
| `Assets/Scripts/Skills/SkillUnlocker.cs` | `SkillUnlocker` | `config.RequiredLevel <= _progress.CurrentLevel` |
| `Assets/Scripts/Scriptables/Skills/SkillConfig.cs` | abstract SO | `Icon, Tier(SkillTier), RequiredLevel, Description, Duration=3, Cooldown=8` |
| `Assets/Scripts/Scriptables/Skills/AttractConfig.cs` | SO | `AttractionForce=6, ApproachMultiplier=3, ApproachPower=2` |
| `Assets/Scripts/Scriptables/Skills/SkillsConfig.cs` | SO | каталог скиллов (потребитель — LevelRewardPopup) |
| `Assets/Scripts/Skills/SkillTier.cs` | enum | Low/Medium/High — декорация |

**На что смотреть:**
- Таймер скилла стоит на паузе (гейт `Timer`) — а вот ввод НЕ замирает: `TryActivate` возможен на паузе.
- Валидаций нет: `SkillHandler`, `SkillInputBinder` (4 ссылки), `SkillUnlocker` (false при null-progress = скилл молча заблокирован навсегда).
- `AttractSkill` валидирует ассет в Awake (M≥1, P>0) — валидация данных живёт в рантайме, не в SO.
- Сравнение конфигов по ссылке: ассет `AttractConfig` в SkillInputBinder и SkillsConfig должен быть ОДИН и тот же.
- Пустые `OnActivated/OnTick/OnDeactivated` — вся механика в обработчике детекта (осознанная форма, не мусор).

---

## 13. Камера

| Файл | Класс | Роль |
|---|---|---|
| `Assets/Scripts/Camera/CameraFollow.cs` | `CameraFollow` | LateUpdate: `SmoothDamp` к `target + offset.normalized * Max(1, offsetLen - impulse.Pull)`; офсет тира = `_startOffset * GetCameraOffsetFor(tier)` (в OnEnable — стартовый тир учтён) |
| `Assets/Scripts/Camera/CameraImpulse.cs` | `CameraImpulse` | `_pull`: + по `LevelProgress.ItemCollected` (кривая по BaseMass, cap MaxPull — придвигает), − по росту тира (до -MaxPush — отодвигает); экспоненциальный спад `1 - exp(-RecoverSpeed*dt)` |
| `Assets/Scripts/Scriptables/Camera/CameraImpulseConfig.cs` | SO | кривая `MassToPullStrength(0→0.4, 50→2.5)`, MaxPull=6, TierPushStrength=4, MaxPush=8, RecoverSpeed=4 |

**На что смотреть:** связка через SerializeField (`_impulse` + свойство `Pull`); положительный Pull придвигает камеру; `RecoverSpeed <= 0` в ассете сломает спад (валидации нет); null-инъекции не валидируются (тихо не подпишется); пауза замирает по deltaTime (для камеры корректно).

---

## 14. Магазин и скины

**Ответственность:** каталог скинов, выбор/покупка, превью-модель, применение скина в Game.

| Файл | Класс | Роль |
|---|---|---|
| `Assets/Scripts/Shop/Shop.cs` | `Shop` (RequireComponent ModelPlacer/LevelTransitor/Wallet) | OnEnable: подписки + гард `_isInitialized`; SDK готов → `InitializeShop`, иначе по `YG2.onGetSDKData`; `InitializeShop` = `ShopPanel.Initialize(_wallet)` + `Show(content.SkinItems)` + превью; Close → `LoadScene(YG2.saves.PreviousScene)` |
| `Assets/Scripts/Shop/ShopPanel.cs` | `ShopPanel` | спавн вьюх через фабрику; состояние из `YG2.saves` напрямую (`_openSkins.Contains`, `SelectedSkinType`); `TryUnlock` (баланс-чек → `Spend` при Price>0 → Add в `_openSkins` → `YG2.SaveProgress()`); `SelectPersist`; баланс в TMP; `Clear` в OnDisable |
| `Assets/Scripts/Shop/ShopItemView.cs` | `ShopItemView` | карточка: иконка/цена/замок/выделение/хайлайт-фон; `event Click`; RequireComponent Image/Button |
| `Assets/Scripts/Shop/ShopItemViewFactory.cs` | `ShopItemViewFactory` | Instantiate + Initialize (класс переименован из SkinItemViewFactory — лечит missing script, волна 1) |
| `Assets/Scripts/Shop/ModelPlacer.cs` | `ModelPlacer` | превью-модель перед ортокамерой (фит по 8-угловым баундам → orthographicSize), вращение на `unscaledDeltaTime`, `Animator.SetTrigger("Walk")` |
| `Assets/Scripts/Scriptables/Shop/ShopContent.cs` | SO | `List<SkinItem>`; OnValidate кидает на дубли SkinType |
| `Assets/Scripts/Scriptables/Skins/ShopItem.cs` | SO `SkinItem` | `Model, Icon, Price[0..10000], SkinType` |
| `Assets/Scripts/Player/PlayerSkins.cs` | enum | `Slime, Pacman, TripleT` — ключ сейвов (переименование значений = потеря прогресса) |
| `Assets/Scripts/Player/SkinApplier.cs` | `SkinApplier` (в Game.unity) | Start: инстанс модели выбранного скина в контейнер по `ShopContent` |

**На что смотреть:**
- Персист идёт МИМО PlayerProgress — напрямую `YG2.saves` + `YG2.SaveProgress()` (асимметрия шва).
- Гард `_isInitialized` никогда не сбрасывается; повторный `ShopPanel.Initialize` отписывает прежний кошелёк (идемпотентно).
- `ViewSelected` кидается ДО проверки замка — превью показывается и для залоченного (дизайн).
- `ShopItemView.OnDisable` — NRE при disable до Initialize (узкое окно); двойной `Initialize` без пере-disable = двойной listener.
- `ModelPlacer` вращает на unscaled-времени (живёт на паузе); `ComputeLocalBounds` лезет в `renderers[0]` без проверки длины.
- Wallet приходит компонентом с того же GO + ShopLifetimeScope регистрирует его (волна 1: без этого сейв-экономика падала).

---

## 15. Fill-сессия

**Ответственность:** оркестрация заливки (тема → сетка по текстуре → кубы → процент), награда, переходы, реклама/лидерборд.

| Файл | Класс | Роль |
|---|---|---|
| `Assets/Scripts/Game/FillSessionHandler.cs` | `FillSessionHandler` | Awake: fail-fast, создаёт `YandexAdsBridge.Create()` → Setup обоим; OnEnable: подписки `FillCompleted`/`RewardGranted`; Start: `ApplyTheme` (`Theme.FillShapeTexture` → GridBuilder, мягко) → `StartFill`; `percent >= 1` → RewardWin, иначе RewardLose → `Win/Failed`; `LoadNextLevel` (level++, MaxLevel, Save, Report, интерстишл, LoadGame), `RestartLevel` |
| `Assets/Scripts/ShapeFill/ShapeFillOrchestrator.cs` | фасад (RequireComponent GridBuilder/ShapeFiller/FillCounter) | `StartFill()` = Initialize → Build → BuildShape → `Fill(FillCounter.CalculateFill(cells))`; переиздаёт `FillCompleted(float)` |
| `Assets/Scripts/ShapeFill/GridBuilder.cs` | `GridBuilder` | растеризация Texture2D (`GetPixels`, разрешение 64, alpha > 0.1), бордер (толщина 2), сортировка сверху-вниз, `GridToWorld`, `GetPixelColor` |
| `Assets/Scripts/ShapeFill/ShapeFiller.cs` | `ShapeFiller` | ghost-подложка (`Sprite.Create`), бордер из кубов, UniTask-спавн каждые 0.04 с, считает прибывших → `FillCompleted(target/Required)`; событие `CubeArrived` |
| `Assets/Scripts/ShapeFill/CubeSpawner.cs` | `CubeSpawner` | Instantiate куба + `MaterialPropertyBlock.SetColor("_Color", pixel)` (пула нет) |
| `Assets/Scripts/ShapeFill/FlyingCube.cs` | `FlyingCube` | smoothstep-полёт в клетку, событие `Arrived` |
| `Assets/Scripts/ShapeFill/FillCounter.cs` | `FillCounter` (DI) | `RoundToInt(LevelProgress.FillPercent * maxCubes)` — единственный [Inject] кластера |

**На что смотреть:**
- Сетка строится дважды за StartFill (в Orchestrator и внутри BuildShape) — аллокации `GetPixels` ×2.
- `Sprite.Create` в PlaceGhost без уничтожения старого — утечка на каждый Build.
- Рестарт заливки: `StopFill` не снимает летящие кубы и их подписки → ложный/досрочный `FillCompleted` на повторном Fill.
- Кубы не деспавнятся никогда — накапливаются под `_cubesParent`.
- `GridBuilder` требует Read/Write у текстуры; null-текстура → NRE (тема с null-текстурой handled мягко в FillSessionHandler).
- `LoadNextLevel`: реклама показывается ДО смены сцены.
- Требование `percent >= 1` в RewardWin согласовано с гейтом в OnFillCompleted.

---

## 16. UI

**Ответственность:** HUD (только рендер событий домена), модальные окна (пауза через BaseWindow), фабрики окон, локализация UI, вспомогательные view.

| Файл | Класс | Роль |
|---|---|---|
| `Assets/Scripts/UI/Spawners/GameplayUIFabric.cs` | Game | кнопки стартового канваса (пауза/лидерборд/магазин → `LoadShop`), `GameStarted` → HideButtons; спавн PauseMenu (`showRestart: true`), LeaderboardMenu |
| `Assets/Scripts/UI/Spawners/FillUIFabric.cs` | Fill | `Win` → WinMenu(+LevelRewardPopup по `SkillsConfig.RequiredLevel`), `Failed` → FailMenu; экшены: удвоение (`ShowDoubleReward` → `Wallet.Add(_lastRewardAmount)`), след. уровень за rewarded (`NextLevelRewardId`), рестарт (`TryShowInterstitial` + RestartLevel); `[Inject] PlayerProgress` |
| `Assets/Scripts/UI/Windows/BaseWindow.cs` | НЕ sealed | `Initialize(Pauser)` → пауза; `OnDisable` → `Pauser?.RequestResume()`; закрытие окон = `Destroy(gameObject)` |
| `Assets/Scripts/UI/Windows/PauseMenu.cs` | | закрыть/рестарт (опционален)/`AudioSettingsPanel.Initialize(mixer)` |
| `Assets/Scripts/UI/Windows/WinMenu.cs` | | награда, next (LoadNextLevel), «x2» (одноразовая кнопка) |
| `Assets/Scripts/UI/Windows/FailMenu.cs` | | награда, рестарт, «next за рекламу» (`_nextLevelButtonForADS`) |
| `Assets/Scripts/UI/Windows/LeaderboardMenu.cs` | | свой YandexAdsBridge, топ-10 + строка игрока (`<b>`), ключи `leaderboard_*`, уничтожение бриджа в OnDisable |
| `Assets/Scripts/UI/Windows/LevelRewardPopup.cs` | НЕ наследник BaseWindow | попап «скилл открыт», паузу НЕ ставит; валидаций нет |
| `Assets/Scripts/UI/HUD/QuotaUI.cs` | `[Inject] LevelProgress` | Populate по `LevelProgress.Quota` + `QuotaChanged` → пластины (ленивое создание); гард `_isSubscribed` |
| `Assets/Scripts/UI/HUD/QuotaPlateUI.cs` | | иконка + бейдж (`TierTable.ShortLabel/BadgeColor`) + остаток |
| `Assets/Scripts/UI/HUD/GrowthBarView.cs` | | `MassChanged/TierChanged` → fill (`GetTierProgress`) + текст `Localization.Get("tier_*")` |
| `Assets/Scripts/UI/HUD/MassUI.cs` | | число массы; **без валидаций + нет ссылок из сцен** (§19) |
| `Assets/Scripts/UI/HUD/TimerUI.cs` | | `Timer.Ticked` → `{0.0}` с clamp |
| `Assets/Scripts/UI/HUD/LevelLabelUI.cs` | `[Inject] PlayerProgress` | `Localization.Get("level_label")` + LanguageChanged (намеренно мягкие проверки) |
| `Assets/Scripts/UI/Common/ValueView.cs` / `IntValueView.cs` | generic / sealed | `Show(T)` = SetActive(true) + text |
| `Assets/Scripts/UI/LocalizedText.cs` | | ключ в инспекторе → TMP; перерисовка по LanguageChanged; пустой ключ → throw из OnEnable |
| `Assets/Scripts/UI/LanguageSwitcher.cs` | | `CycleLanguage()`, самоподпись `lang_self_*` |
| `Assets/Scripts/UI/LookAtCamera.cs` | | билборд-rotation в LateUpdate |

**На что смотреть:**
- Новые ключи локализации: строка в `Localization.asset` → `_key` на LocalizedText или `Localization.Get` (волна 3).
- `GrowthBarView` НЕ подписан на LanguageChanged — текст тира обновится только при следующей смене массы (единственный текст HUD вне цикла языка).
- Fail-fast дыры: `MassUI`, `LevelRewardPopup`, `PauseMenu._settingsPanel/_closeButton`, фабрики (не валидируют префабы/mixer/pauser), `LevelLabelUI` (намеренно мягкий).
- `WinMenu`/`FailMenu`: RemoveListener+AddListener (идемпотентный Initialize), base.Initialize ПОСЛЕДНИМ (пауза после валидаций).
- OnGameWin спавнит WinMenu и LevelRewardPopup сразу — попап не паузит и не является BaseWindow.
- Спавн окон — в корень сцены, без контейнера.

---

## 17. Аудио

| Файл | Роль |
|---|---|
| `Assets/Scripts/Audio/AudioMixerController.cs` | владелец громкости: `SetMusicVolume/SetSFXVolume` → `mixer.SetFloat` (dB = 20·log10, guard 0.0001) + мгновенная запись в `YG2.saves` + дебаунс `SaveProgress` 500 мс (CTS + UniTask.Delay); применение из сейвов в Awake и по `YG2.onGetSDKData` |
| `Assets/Scripts/Audio/AudioSettingsPanel.cs` | слайдеры паузы: Initialize идемпотентен, стартовые значения ДО AddListener (нет ложного сейва) |
| `Assets/Scripts/Audio/SoundLimiter.cs` | счётчик одновременных (`_maxConcurrent=6`), `TryPlay(duration)` + UniTask.Delay с токеном уничтожения |
| `Assets/Scripts/Audio/PlayerPickupSound.cs` | `Collector.ItemCollected`; двойной дебаунс (интервал 0.1 + лимитер), pitch 0.96–1.1 |
| `Assets/Scripts/Audio/TierUpSound.cs` | `PlayerTier.TierChanged` только вверх; AudioSource добавляется кодом; `_clip` владельцу подставить |
| `Assets/Scripts/Audio/TimerTickSound.cs` | loop-тик при remaining ≤ 20 с; флаг идемпотентности; страховка `Finished` |
| `Assets/Scripts/Audio/LevelMusicPlayer.cs` | loop-музыка уровня (без событий) |
| `Assets/Scripts/Audio/FlyingCubeArrivalSound.cs` | `ShapeFiller.CubeArrived` на каждый куб, БЕЗ лимитера — наслаивание |
| `Assets/Scripts/Audio/UIButtonSound.cs` | клик на список кнопок; без лимитера; `AddButton` без защиты от дублей |

**На что смотреть:** общий паттерн — `[RequireComponent(AudioSource)]` (кроме TierUpSound), настройка источника в Awake, подписки OnEnable/OnDisable. `_group` нигде не валидируется (null → звук мимо шины SFX). Никто не глушит звук на паузе (тик/музыка играют при timeScale=0). Хвост дебаунса: изменение громкости < 500 мс до OnDestroy не сохранится. Грязь: `[SerializeField]private  List<Button>` в UIButtonSound.

---

## 18. Editor-инструменты

| Файл | Роль |
|---|---|
| `Assets/Editor/ItemPropFactory.cs` | «Mad Slime → Prop Factory»: единый конвейер Generate (волна 18): иконки из МОДЕЛЕЙ (изоляция по свободному слою 9–31, ortho RT 256², Force Icons для перегенерации; ручной PNG перекрывает) → `EnsureSpriteImport` (TextureImporter → Sprite, форсится) → дефинишны `D_Item_<Model>_<Tier>.asset` (SerializedObject: `_tier/_baseMass/_icon`; fallback Small) → обёртки `Item_<Model>.prefab` (get-or-update через LoadPrefabContents: слой Collectable, BoxCollider по рендер-баундам — инсета нет, волна 18-отмена; `Item` с `_definition/_collider/_ghostMaterial/_ghostFadeConfig`) → `WritePropSet` (пересборка `_props`+`_variants` целиком). Состояние окна в EditorPrefs (`MadSlime.ItemPropFactory`). Превалидация — LogError + abort (дубли имён, модель без Renderer, нет слоя Collectable, пустой TierTable) |
| `Assets/Scripts/Game/Level/LayoutPreviewDrawer.cs` | гизмо в Game.unity (#if UNITY_EDITOR): границы карты, все лейауты библиотеки или `_customLayout`, детерминированные точки (seed), метки `L{i} / Zone j`; публичные свойства для editor-хэндлов; при null `_propSet` — NRE в гизмо |
| `Assets/Editor/LayoutPreviewDrawerEditor.cs` | ручки `FreeMoveHandle` на `_zones[i]._center` (Custom Layout приоритетнее, иначе `Handle Layout Index`), запись через SerializedObject с Undo, зеркалирование в обе стороны |

**На что смотреть:** тулза НЕ переключает сцены (волна 18, фиксы 2–4) — работает в любой открытой сцене; сохранения точечные (`SaveAssetIfDirty(_propSet)`), глобальных SaveAssets/Refresh-штормов нет; get-or-create по путям — гуиды живут между прогонами; модели с уже существующим `Item` скипаются (обёртки не ре-обрабатываются как модели); undo не пишется в дефинишнах (WithoutUndo). Рабочий процесс владельца: 2 прогона (Room, Table), после — проверить иконки.

---

## 19. Открытые хвосты, мёртвое и кандидаты на чистку

**Временное:**
- `[Diag]`-логи (§2.7) — снести после подтверждения владельцем (волны 22–25).
- Компиляция волн 20–25 не прогнана batchmode (редактор был открыт) — прогнать при первой возможности.

**Ждут решения/действий владельца (не код):**
- Лидерборд `max_level` в Яндекс.Консоли (тип «максимальный»).
- `TierUpSound._clip` пуст — подставить в инспекторе (Game → Player).
- WebGL-сборка на Яндексе: реклама/лидерборд/язык живут только в билде.
- Данные уровней: RoomLevelConfig не разведён с Table (обa диапазона каталога → Table), RoomLayoutSet пуст, снежный ком TableLayoutSet (волна 19): кандидаты — зоны под карту 30×30 / поднять mapSize / гард-warning в генераторе.
- 14 старых `D_*.asset` в корне `Scriptables/Items/` уже снесены (актуально: там только `GhostFadeConfig.asset` + `ItemsDefinition/`).

**Мёртвое/подозрительное (найдено проходом, не заявлено в журнале):**
- `Assets/Scripts/UI/HUD/MassUI.cs` — НЕ ссылается ни одна сцена/префаб (проверено grep гуида по всем .unity/.prefab). Кандидат на удаление или на возврат в HUD.
- `Assets/Resources/Prefabs/UI/Menu/DeathMenu.prefab` — остаток v1 (внутри только UIButtonSound); fabrics его не спавнят.
- `Wallet`-компонент в Game.unity — не регистрируется в GameLifetimeScope → `[Inject]` в Game-сцене не выполнится; потребителей Wallet в Game-сцене по коду нет. Рудимент проводки.
- `Assets/Scenes/Test.unity` — вне билда.
- `SkillTier` enum — читается только самим SkillConfig (декорация).
- `Assets/amusedART/.../Sc_AniTest.cs`, `Assets/Quirky Series Birds Bundle` — сторонние ассеты-демо, не проектный код.

**Тех. долг (из журнала):**
- `UniTask` без пина коммита в manifest.json; asmdef/тестов нет.
- Схема сейва без версионирования.
- Два несведённых источника рандома генерации (§2.8).
- `AdScheduler`/`ShapeFiller` — рестарт-гоники (§6, §15).

---

## 20. Ревью-панч-лист (стиль, нейминг, структура) — 2026-09-16

Полный проход по всем 123 собственным `.cs` с прицелом на нейминг / модификаторы / форматирование / абстракции. Пересечения с §1–19 помечены; здесь — сгущённый чек-лист правок в порядке приоритета. Вне ревью: `SavesYG`, `PlayerInputActions`, JS-приёмники `YandexAdsBridge` (контракты, не стиль).

### 20.1 Поведенческое (чинить первым)

1. **`MoveChecker.cs:32` — локальный радиус в мировом касте.** SphereCast берёт `collider.radius` (0.5) без учёта lossyScale. §9 считает, что при scale=1 иерархии это сходится; волна 25 исходила из lossyScale ~52 (иначе ghost-фикс не нужен). Арбитр — `[Diag] worldRadius` гост-тогглера после волны 25: печатает `0.5` → капсула в scale 1, пункт снят (и перепроверить трактовку §11); печатает `~26` → умножать и здесь.
2. **`GridBuilder.Build()` ×2 за заливку** — `ShapeFillOrchestrator.StartFill:37` и `ShapeFiller.BuildShape:60` (§15). Убрать один вызов.
3. **`ShapeFiller` — флаг вместо отмены**: `StopFill` лишь ставит `_isFilling = false`; старый `FillAsync` может пройти проверку флага, зависнуть в `Delay` и пересечься с новым циклом на общем `_fillIndex`. CTS на цикл по образцу `Timer` (§15).
4. **`PlayerTier.Add:70` — `TierChanged` без смены тира** (§10). Огонь только при `currentTier != previousTier` → сносятся самодельные guard'ы `LevelScaler.OnTierChanged:104`, `CameraImpulse.OnTierChanged:101`, `TierUpSound:43`.
5. **Дыры fail-fast — добить (§2.1):** `Player.Awake:38` (молчаливый return при null `PlayerConfig`; не валидирует `_inputReader`/`_collector`), `GenericOverlapDetector.OnEnable:53` (молчаливое отключение tier-логики), `SkinApplier.ApplySelectedSkin:53`, `SkillUnlocker.IsUnlocked:19`, `LevelLabelUI.UpdateLabel:38`.
6. **`AttractableDetector` молотит OverlapSphere каждый кадр при кулдауне скилла** — фильтр только в `AttractSkill.OnAttractableDetected:82`. Гейтить `IsActive` на уровне детектора/подписки (при 2845 предметах — постоянная холостая работа).
7. **Магическая константа выигрыша**: `FillSessionHandler.OnFillCompleted:120` (`>= 1f`) дублирует `Rewarder.RewardWin:31` (`< 1f`). Один источник истины (константа либо bool от ShapeFiller).
8. **`ShapeFiller.PlaceGhost:98`** — `Sprite.Create` без `Destroy` старого спрайта (§15).

### 20.2 Нейминг

| Где | Сейчас → предлагается |
|---|---|
| `FailMenu.cs:13` | `_nextLevelButtonForADS` → `_rewardedNextLevelButton` (капс + шум) |
| `ShopContent.cs:17` | `skinDuplikates` → `duplicateGroups` (опечатка) |
| `MassUI.cs:27` | `OnTierChanged` — хендлер **Mass**Changed → `OnMassChanged` |
| `LevelScaler.cs:23-25` | `_baseController*` (это капсула, не CharacterController) → `_baseCollider*` |
| `ShapeFiller.cs:12` | `_gridShape: GridBuilder` → `_gridBuilder` |
| `ShopItemView.cs:20,29` | `_selectionText` (это Image) → `_selectionIcon`; `IsLock` → `IsLocked` |
| `ShopPanel.cs:14`, `WinMenu.cs:11`, `FailMenu.cs:11` | «money» при доменном `Balance` (Wallet.Balance) → `_balanceText` |
| `AudioMixerController` vs `PlayerProgress` vs сейв | `SFXVolume` / `SfxVolume` / `sfxVolume` — свести к одному написанию |
| `Skills/ItemTier.cs` | tier предметов живёт в `Skills`, потребители — Items/Collectables/Detection/Player/Camera → переезд в `Items` отдельной волной |

### 20.3 Модификаторы / sealed

- `ShopItemView.cs:10`, `Scriptables/Shop/ShopContent.cs`, `Scriptables/Skins/ShopItem.cs` — `public class` без `sealed`, наследников нет (`ValueView<T>`/`BaseWindow` — законные исключения).
- `ShopItemView.OnClick:56` — public обработчик, нужен только Button'у → private.
- `IMassHolder.cs:5` — `public` на члене интерфейса лишний.

### 20.4 Форматирование

- `UIButtonSound.cs:14` — `[SerializeField]private  List<Button>` (нет пробела после `]`, двойной после `private`); `:58` — whitespace-строка; `:29` — сообщение `buttons is empty` вне стиля.
- Whitespace-строки вместо разделителей: `TierResolver.cs:98`, `ShopItemView.cs:39`.
- Голые bool в `if` против канона `== true/false`: `Shop.cs:55,76`.
- Нет финального newline: `ShopContent.cs`, `ShopItem.cs`, `TierScalerConfig.cs`, `AudioSettingsPanel.cs`.

### 20.5 Дубли (не хватает абстракции)

- **DI-валидация**: `Project` (пять одинаковых if) и `Shop` пишут проверки руками; у `Game`/`Fill` есть `ValidateAssigned`. Один хелпер на все четыре скоупа.
- **`TierResolver.cs:46-123`**: `GetSpeedFor/GetScaleFor/GetCameraOffsetFor` — три копии одного поиска → приватный `Find(Tier)`. Фолбэки разные: `4f`/`1f`/`1f` — магическая скорость 4 нигде не объявлена.
- **Окна**: `Close() => Destroy(gameObject)` в WinMenu/FailMenu/PauseMenu/LeaderboardMenu — поднять в `BaseWindow`.
- **Политика валидации serialized-полей** непоследовательна: QuotaUI/GrowthBarView — каждое поле; GameplayUIFabric/FillUIFabric — 2 из 10 (`_pauser`, `_levelTransitor`, `_mixerController`, префабы не проверяются); MassUI/LevelRewardPopup — ноль. Выбрать одну (канон §2.1).
- Два источника рандома — уже §2.8, здесь не дублируется.

### 20.6 Замудрено (упростить)

- **`AdScheduler.cs:13-15`**: три поля состояния rewarded (`_rewardedReceived` + две pending-акции) → один колбэк `onClosed(bool granted)`; заодно закрывает «повторный ShowRewarded перетирает pending» (§6).
- **`QuotaUI`**: `_plates` + `_platesByEntry` — список нужен только ради `Count` → оставить словарь.
- **`Timer.cs:82-91`**: linked CTS поверх destroy-токена + dispose в трёх местах → один владелец.
- **`ModelPlacer`**: два подхода к bounds в одном классе (8-угловый `AccumulateByCorners` и упрощённый `ComputeLocalBounds`) — унифицировать.
- **`LevelProgress.FillPercent`**: `extraWeight`/`_defaultCountDivisor` — бизнес-правило «лишние предметы = бонусная квота» не читается; имя `bonusQuotaCount` честнее.
- **`AudioSettingsPanel.Initialize:30`**: присваивание `_mixerController` ДО null-check — при throw поле перезаписано; валидация до присваивания.

### 20.7 Точечные нарушения AI_RULES

- **`ShopContent.cs` — вне канона целиком**: `var` (единственный в проекте), LINQ-цепочка с лямбдами, `throw` в `OnValidate`, класс не sealed. Переписать циклом.
- Тернарники: `LevelConfigResolver.cs:39`, `TierTable.cs:43` (`ShortLabel`).
- `?? throw`: `AdScheduler.cs:34`, `LeaderboardReporter.cs:30` — единственные в проекте (везде if/throw).
- **Структура**: `Scriptables/Tiers/` и `Scriptables/Tier/` — два соседних каталога в ед./мн. числе (слить); `Scripts/Shop/` → `namespace Skins` и `Scriptables/Items/GhostFadeConfig.cs` → `namespace Items` при остальном `Scriptables`; файл `ShopItem.cs` содержит класс `SkinItem` (файл ≠ класс).

### 20.8 Порядок работ

20.1 (поведение) → 20.7 (`ShopContent` + структура — дёшево и заметно) → 20.2/20.5 → 20.3/20.4/20.6. Переименования serialized-полей (`_nextLevelButtonForADS`, `_gridShape`, `_baseController*`) — только вместе с правкой YAML сцены/префабов (гайд `.agents/skills/unity-yaml-editing-guide`), иначе отвалится проводка. Переезд `ItemTier` и неймспейсов `Scriptables` — отдельной волной.
