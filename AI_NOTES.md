# AI_NOTES — рабочий журнал сессии (для следующего агента)

> Ведёт Claude Code, сессия «ультракод» от 2026-09-09/10. Обновляется по ходу. Читай перед продолжением работы.

## ⚠️ ВЕТКИ
- Целевой проект — ветка **`MadSlimeV2`**. Worktree-сессия `orca/workspaces/Mad-Slime/MadSlime` fast-forward'нута на `MadSlimeV2` (`9576d8a`). Основной чекаут `~/Repos/Unity/Mad-Slime` — тоже MadSlimeV2.
- Первое ревью делалось по базе `875ca4f` (не зная о v2) — потом триажено. Итоговый список ниже — только то, что АКТУАЛЬНО для v2.
- v2: VContainer DI (осознанно, НЕ баг), single-сцены `Game`/`Fill`/`Shop` (Level1-4 снесены, уровни генерируются `LevelGenerator` + `LevelsCatalog`), враги/Health/QuotaTracker/Levels-карта/Dodge-Sprint удалены. 115 .cs.

## Факты о проекте
- Unity 2022.3.62f2 локально: `~/Unity/Hub/Editor/2022.3.62f2`, бинарь `~/.local/bin/unity`. Компиляция batchmode (Temp/UnityLockfile отсутствует).
- Стек: VContainer 1.18.0, UniTask (git, плавающий пин — риск), Input System, TMP, uGUI, YG2 (сейвы/реклама/лидерборд), WebGL/Yandex, stripping High.
- Сейвы: `SavesYG` partial (YG-конвенция, поля = JSON-ключи; `_openSkins` с underscore НЕ переименовывать — потеря прогресса). Шов над сейвом: `PlayerProgress` (+ `Save()` с guard isSDKEnabled).
- Автогенерат `PlayerInput/PlayerInputActions.cs` — не править руками. Дубль-мутант `Assets/Settings/Input/PlayerInputActions.inputactions` — не тот файл.
- Установлены Unity-скиллы: `.agents/skills/` + `.claude/skills/` (nowsprinting/unity-coding-skills, не закоммичены) + `skills-lock.json`.

## ИТОГОВЫЙ СПИСОК НАХОДОК v2 (ревью завершено)

### A. Behavior-баги (подтверждены верификацией/лично) — ждут решения владельца
1. **CRITICAL, магазин сломан**: класс `SkinItemViewFactory` в файле `ShopItemViewFactory.cs` → Unity резолвит MonoScript по имени файла → missing script в `Shop.prefab:108` → `ShopPanel._factory` = null → NRE в `Show()`. Фикс: переименовать класс в `ShopItemViewFactory` (guid сохранится, ссылка восстановится сама) + тип поля в `ShopPanel.cs:12`. Это восстановление сломанного — делается без вопросов.
2. **`AudioMixerController.cs:93,108`**: `YG2.SaveProgress()` на каждый тик слайдера (каждый кадр драга) → сериализация+JS-мост Яндекса, раздувает `idSave`. Фикс: дебаунс (CTS + `UniTask.Delay(500)`, один общий на оба слайдера), либо сохранение в `OnDisable` панели.
3. **`AudioMixerController.cs:45`**: громкость не применяется к микшеру до готовности SDK и никогда при `isSDKEnabled == false` (editor) — в редакторе настройки звука мертвы. Фикс: применять всегда, сохранять только при SDK.
4. **WinMenu/FailMenu вне BaseWindow**: в префабах `_pauser: {fileID: 0}` (null), `RequestResume()` без guard в OnDisable, AddListener до присвоения pauser. Фикс: наследовать BaseWindow (+ null-безопасность там уже есть).
5. **`GameplayUIFabric`: подписки в Awake, отписка в OnDisable** — после первого disable кнопки/события мертвы (латентно). Фикс: перенести в OnEnable. То же: `FillSessionHandler` (подписки в Start), `ShapeFillOrchestrator.StartFill` (подписка на каждый вызов).
6. **`Shop.cs`**: (а) `InitializeShop` не guarded на пути OnEnable → дубль подписки BalanceChanged; (б) NRE `OnViewSelected(null)` при `SelectedView == null` (стейл-сейв/удалённый скин); (в) подписки в Awake/отписка в OnDisable; (г) `ApplySelection` пишет сейв при каждом открытии магазина + двойной сейв при покупке. Фиксы: гард в InitializeShop, null-guard, подписки в OnEnable, персист только при реальном выборе.
7. **`SkinUnlocker`**: нет guard «уже открыт» + `Spend(0)` кидает при Price=0. (Visitor-слой сносится — см. B, фиксы переезжают в новые методы.)
8. **`SoundLimiter`**: `UniTask.Delay` без токена — переживает destroy. Фикс: `this.GetCancellationTokenOnDestroy()`.
9. **`AudioSettingsPanel`**: Initialize/AddListener асимметрия с OnDisable (дубль listener при повторном Initialize — латентно).
10. **`CameraFollow`**: `_playerTier` не валидируется в Awake (NRE в OnEnable); нет pull начального offset по текущему тиру (старт с высоким default-тиром → камера не отъедет до первого TierChanged). Фикс: валидация + pull в OnEnable.
11. **Валидации serialized-полей**: `BaseSkill._timer`, `AttractSkill._detector/_playerTier`, `GrowthBarView._playerTier/_tierResolver/_progressBar/_tierText`, `MoveChecker._playerCollider/_playerTier`, `QuotaUI._platePrefab/_container`, `ModelPlacer` (vector3[8] аллокация на каждый renderer + мёртвая проверка `_modelsParent` в Update).
12. **`PlayerInputReader`**: нет `Dispose()` input-ассета в OnDestroy.

### B. Структурный рефакторинг (поведение не меняет — делаем без вопросов)
- **Мёртвый код — удалить**: `Item/CircleItemSpawner.cs`, `ItemGridSpawner.cs`, `ItemSpawner.cs` (0 ссылок в сценах/префабах/коде — заменены `LevelGenerator`); `Item.Collected` (0 подписчиков); `ShopLifetimeScope.cs` (в сцене Shop нет DI-скоупа — ВНИМАНИЕ: спросить владельца — удалить или подключить); `Pauser` лишнее условие `_pauseRequestCount >= 1`.
- **Visitor-фикция в Shop** (`Visitors/`, 5 файлов): один visitable-тип, нет Accept/double-dispatch, stateful `Result` протухает в цикле `Show`. Снести → прямые методы в `ShopPanel` (`IsOpen/IsSelected/Select/TryUnlock`), `SkinUnlocker` оставить как единственный класс покупки с Wallet.
- **Неймспейсы → по папкам**: `LevelScaler` Skills→Player; `MoveChecker` глобал→Movement; `ModelPlacer` глобал→Skins; `SkinApplier` глобал→Player. `HealthSystem`/`CameraSystem`/`Skins` — осознанно оставить.
- **sealed** для leaf-классов (~20: FillUIFabric, GameplayUIFabric, FailMenu, WinMenu, QuotaUI... — WinMenu/FailMenu станут sealed при переводе на BaseWindow).
- **Dead usings**: PlayerInputReader (`using System;`), ValueView и пр. — сверкнуть по всем.
- **Отступы 4-vs-8 + хвостовые пробелы**: Rotator, CameraFollow, ValueView; `Item.cs:17` `Tier =>Definition`; `TMPro.TMP_Text` fully-qualified в ShopPanel/LevelRewardPopup; ModelPlacer `!result.HasValue`.
- **DRY**: `LayoutPreviewDrawer` дублирует математику `LevelGenerator` (Grid/Circle/CircleGrid collect) — вынести общий планировщик позиций; `MoveChecker` — гизмо каждый кадр + сайд-эффекты в предикате `_lastPosition/_lastVelocity`.
- **FillUIFabric/GameplayUIFabric** — не sealed; GameplayUIFabric `_buttonsCanvas` скрывает канвас — ок.

### C. Факты владельцу (не чиним молча)
- **Реклама и лидерборд вырезаны компиляцией**: defines `RewardedAdv_yg`/`InterstitialAdv_yg`/`Leaderboard_yg` не объявлены ни для одной платформы → `AdScheduler.ShowDoubleReward` в сборке тихо ничего не делает (кнопка «удвоить награду» — no-op), лидерборд не репортится. Проверить настройки YG2-hub.
- **MoveChecker-маска**:Movement-гейт читает только «Collectable»-слой; надо проверить m_Bits в `Game.unity` — если стены на другом слое, игрок проходит сквозь борды. Проверить в рефакторинге, решить с владельцем.
- **Локализации нет** вообще (платформа мультиязычная) — строки захардкожены (RU). Не рефакторинг-тема, просто факт.
- Схема сейва в partial-классе плагина без версионирования; `UniTask` без пина коммита в manifest.json.
- `LevelGenerator`/preview дублируют scatter-рандом (Unity Random vs seeded System.Random) — превью не совпадает со scatter-генерацией осознанно.

## Решения/принципы рефакторинга
- Канон стиля — `AI_RULES.md`. VContainer DI — норма проекта в v2, не трогать.
- Переименования MonoBehaviour-классов/файлов: `git mv` + правка класса, GUID живёт в `.meta`. Переименовывать serialized-поля только вместе с правкой prefab/scene YAML (гайд `.agents/skills/unity-yaml-editing-guide/SKILL.md`).
- После правок: компиляция batchmode + выборочная проверка ссылок в префабах (missing script).

## Открытые вопросы владельцу (заданы 2026-09-10)
1. Чинить весь список A (behavior)? — **ОДОБРЕНО** («фиксишь баги и везде валидируешь… сразу эксепшн на любой чих» — политика fail-fast зафиксирована).
2. Мёртвые файлы: спавнеры + Item.Collected — **удалены**; `ShopLifetimeScope` — не удалил, а ПОДКЛЮЧИЛ (см. фикс 15).
3. MoveChecker/стены — **НЕ тронуто, нужно решение владельца**: маска m_Bits: 8 (только Collectable), 4 борта «Floor» на Default (Game.unity). Но даже добавление слоя в маску не остановит игрока — MoveChecker блокирует только крупные предметы (return false), а Mover двигает transform в обход физики. Для стен нужен: (а) слой стен в маске + (б) в IsAbleToMove return false для не-attractable хитов. Риск: SphereCast может задеть пол — проверять в редакторе.

## СДЕЛАННЫЕ ФИКСЫ (2026-09-10)
1. `ShopItemViewFactory.cs`: класс `SkinItemViewFactory` → `ShopItemViewFactory` (missing script в Shop.prefab вылечен, guid цел); поле `shopItemViewPrefab` → `_shopItemViewPrefab` (+ ключ в префабе, ссылка сохранена).
2. `ShopPanel.cs`: переписан — Visitor-слой снесён (прямые методы IsOpen/IsSelected/SelectPersist/TryUnlock), persist только при реальном выборе (было: сейв при каждом открытии + двойной при покупке), guard «уже открыт» и цена 0 без Spend, идемпотентный Initialize, fail-fast валидации, TMP using, sealed.
3. `Shop.cs`: гард `_isInitialized` внутри InitializeShop (закрывает оба пути), подписки OnEnable/OnDisable (были Awake/OnDisable), null-guard OnViewSelected, fail-fast валидации, sealed.
4. `Shop.prefab` (YAML): добавлен компонент `ShopLifetimeScope` (fileID 7389000000000000001) на корень Panel с `_wallet` → Wallet(&2209933226809809697). ДО этого Wallet с [Inject] нигде не регистрировался → сейв-экономика магазина падала бы с NRE. RootLifetimeScope (ProjectScope) автоспавнится из preloadedAssets — проверено по исходникам VContainer 1.18.0.
5. Сцена Game.unity: `Collector._massHolderSource` (указывал на MOVER — мусорная проводка времён PlayerMass!) → `_tierHolder: {fileID: 7992803093969525695}` (настоящий PlayerTier).
6. `Collector.cs`: async void → `UniTaskVoid` + `.Forget()`; типизированное поле PlayerTier вместо MonoBehaviour; fail-fast; форматирование.
7. `AudioMixerController.cs`: громкость применяется ВСЕГДА (в редакторе тоже), `YG2.SaveProgress` дебаунсится (CTS + UniTask.Delay 500ms, один на оба слайдера), сейв только при isSDKEnabled, отмена при OnDestroy.
8. `AudioSettingsPanel.cs`: Initialize идемпотентен (RemoveListener перед Add).
9. `SoundLimiter.cs`: UniTask.Delay с GetCancellationTokenOnDestroy; TryPlay(<=0) бросает.
10. `WinMenu.cs`/`FailMenu.cs`: переведены на BaseWindow (пауза/резюм через базу, null-безопасно), удалён serialized `_pauser` (был null в префабах), fail-fast, sealed.
11. `GameplayUIFabric.cs`/`FillUIFabric.cs`: подписки перенесены Awake→OnEnable (FillSessionHandler: Start→OnEnable), fail-fast валидации, sealed.
12. `FillSessionHandler.cs`: подписки в OnEnable, StartFill остался в Start; sealed.
13. `ShapeFillOrchestrator.cs`: подписка FillCompleted в OnEnable (была в каждом StartFill).
14. `CameraFollow.cs`: валидация `_playerTier` + начальный pull camera-offset по CurrentTier (старт с высоким тиром теперь корректен), форматирование.
15. Валидации serialized-полей (fail-fast): `BaseSkill._timer`, `AttractSkill._config/_playerTier/_detector`, `GrowthBarView` (4 поля), `QuotaUI._platePrefab/_container`, `MoveChecker._playerTier/_playerCollider`, `Mover`/`Rotator` — уже были.
16. `MoveChecker.cs`: namespace Movement, sealed, OnDrawGizmosSelected вместо OnDrawGizmos.
17. `ModelPlacer.cs`: namespace Skins, sealed, `== false`, кэш Vector3[8], убрана мёртвая проверка в Update.
18. `LevelScaler.cs`: namespace Skills → Player.
19. `PlayerInputReader.cs`: Dispose input-ассета в OnDestroy.
20. Удалены мёртвые: `Item/CircleItemSpawner.cs`, `ItemGridSpawner.cs`, `ItemSpawner.cs` (+.meta, 0 ссылок), событие `Item.Collected` (0 подписчиков), `Shop/Visitors/*` (5 классов + ISkinVisitor + папка .meta).
21. `Pauser.cs`: убрано мёртвое условие после инкремента.
22. Форматирование: Rotator, ValueView, Item.cs (`=>Definition`), IntValueView (многострочно + sealed), Mover (` == false`), LeaderboardMenu sealed, LevelRewardPopup (TMP using).
23. `Absorber.cs`: форматирование + валидация длительности.

## Волна 2 (2026-09-10, по прямому заказу владельца): стены + реклама + лидерборд

### Стены («чтобы не выпасть»)
- `ProjectSettings/TagManager.asset`: новый слой **Wall** (индекс 8).
- `Game.unity`: GO «Floor» (4 борта-BoxCollider, были Default) → слой 8; маска `MoveChecker` m_Bits 8 → **264** (Collectable|Wall).
- `MoveChecker.IsAbleToMove`: теперь **блокирует любые не-attractable хиты** (стены) и крупные предметы (тир > текущего), мелкие предметы проходимы как раньше. Mover двигает transform в обход физики, поэтому блокировка живёт только здесь.
- Предметы остаются на Collectable (проверено: у префабов override m_Layer: 3), пол-«Cube» на Default — в маску не входит.
- ⚠️ Проверить в редакторе: дойти до борта — игрок должен остановиться; не залипает ли на старте (если SphereCast вдруг задевает пол — но пол вне маски, риск низкий).

### Реклама + лидерборд (свой мост, БЕЗ defines плагина)
Важно: в плагине YG2 v2.0092 **нет кода модулей Adv/Leaderboard** (YG2.cs сгенерирован только со Storage+Authorization; методов RewardedAdvShow/NewLeaderboardScore не существует). Добавлять defines `RewardedAdv_yg`/`InterstitialAdv_yg`/`Leaderboard_yg` НЕЛЬЗЯ — упадёт компиляция (плагиновый EventsYG2 зовёт несуществующие методы). Поэтому сделан собственный мост:
- `Assets/Plugins/MadSlimeYandex.jslib` (NEW): `RewardedAdvShowMadSlime_js` / `InterstitialAdvShowMadSlime_js` / `SetLeaderboardScoreMadSlime_js` — зовут глобальный `ysdk` (его ставит бутстрап плагина), колбэки через `unityInstance.SendMessage('YandexAdsBridge', ...)`.
- `Game/Ads/YandexAdsBridge.cs` (NEW): приёмник сообщений + события; флаги `YG2.nowRewardAdv`/`nowInterAdv` держит в синхроне (чтобы плагиновые GamePause/GameplayAPI вели себя корректно). В редакторе — симуляция (rewarded сразу granted, логи).
- `AdScheduler.cs`: без #if, `Setup(bridge)`, rewarded даёт награду ТОЛЬКО если пришёл колбэк onRewarded (флаг `_rewardedReceived`), интерстишл раз в N уровней.
- `LeaderboardReporter.cs`: без #if, fail-fast на пустое имя, `Setup(bridge)`, `Report(maxLevel)`.
- `FillSessionHandler.cs`: Awake создаёт бридж и делает Setup обоим; в `LoadNextLevel` считается `MaxLevel` (новое поле сейва `SavesYG.MaxLevel = 1`, совместимо со старыми сейвами) и репортится **максимальный** уровень; `_leaderboardReporter` больше не под #if.
- `Fill.unity`: на GO FillSessionHandler добавлен компонент `LeaderboardReporter` (fileID 1495407499) с `_leaderboardName: max_level` + ссылка в `_leaderboardReporter`.
- Компиляция batchmode — чисто.

### ⚠️ Требуется от владельца
1. **Создать в Яндекс.Консоли лидерборд с id `max_level`** (тип «максимальный»), иначе репорт будет падать в ошибку (нефатально, пишется в консоль).
2. Прогнать **WebGL-сборку** на Яндексе: реклама/лидерборд живут только в билде (в редакторе — симуляция).
3. Проверить стены в плее (см. выше).

## Статус
- [x] скиллы установлены
- [x] ревью v2
- [x] согласование (fail-fast политика одобрена владельцем)
- [x] рефакторинг кода (список выше)
- [x] волна 2: стены + реклама + лидерборд (свой мост)
- [x] компиляция batchmode — 0 ошибок (прогнано дважды, включая новые файлы)
- [x] синхронизированы AI_CONTEXT.md (state-часть переписана под v2, GDD сохранён) и CLAUDE.md
- Изменения в рабочем дереве НЕ закоммичены — владелец смотрит и коммитит сам (или попросит).

## Волна 3 (2026-09-10): лидерборд-UI, реклама по GDD, локализация RU/EN/TR, авторизация, звук роста, FX поглощения, конвейер пропсов

### Лидерборд (чтение + UI)
- jslib + `YandexAdsBridge.GetLeaderboardEntries(name, top)` → JSON-колбэк `OnEntriesReceived`; DTO `LeaderboardPayload/LeaderboardEntry` в бридже.
- `LeaderboardMenu`: топ-10 + строка игрока (жирным), если `YG2.player.auth == false` — хинт из локализации + кнопка LOGIN (`YG2.OpenAuthDialog()`). Бридж создаётся меню и живёт до закрытия.
- Имя лидерборда: `max_level` (на компоненте в LeaderboardMenu.prefab). **Владельцу: создать лидерборд `max_level` в Яндекс.Консоли.**

### Реклама (по GDD)
- Победа → next level бесплатно; после победы каждые N уровней — интерстишл (было).
- Поражение: кнопка «следующий уровень» (FailMenu `_nextLevelButtonForADS`) теперь показывает **rewarded**, уровень даётся только после колбэка onRewarded (`AdScheduler.ShowRewarded(id, granted, rejected)`); рестарт после поражения → **интерстишл** (`TryShowInterstitial`).
- `AdsConfig`: + `NextLevelRewardId` (дефолт "NextLevel"). В `FillUIFabric` заведён `_adsConfig` (проводка в Fill.unity).

### Локализация RU/EN/TR
- `Scriptables/Localization/LocalizationTable.cs` (SO: key/ru/en/tr, фолбэк tr→en→ru) + ассет `Assets/Scriptables/Localization/Localization.asset`.
- `Game/Localization/Localization.cs` — статический фасад (таблица+язык+событие LanguageChanged; это инфраструктура как YG2-статики), `LocalizationService` (MonoBehaviour на ProjectScope: инициализация из сейва + персист выбора, зарегистрирован в ProjectLifetimeScope).
- `UI/LocalizedText` (key → TMP), `UI/LanguageSwitcher` — **оживил мёртвую кнопку «Русский» в PauseMenu** (цикл ru→en→tr, подписки в коде).
- Язык: `SavesYG.Language` ("" → системный при первом запуске), из `YG2.lang` НЕ брали — его в v2.0092 не существует (Localization-модуль плагина не установлен).
- Локализовано: Win/Fail (титул, «Заработано:»), метка уровня, названия тиров (tier_Small…), хинты лидерборда, названия языков. `x2`/кнопки-иконки — универсальные.
- **Новый контент добавлять так**: ключ в Localization.asset → `_key` на LocalizedText в префабе, или `Localization.Get("key")` в коде.

### Авторизация (опциональная)
- Аноним по умолчанию; логин — по кнопке в LeaderboardMenu (`YG2.OpenAuthDialog()`, состояние `YG2.player.auth`). Без логина всё играется, просто очки в рейтинг Яндекса не пишутся.

### Звук роста + FX поглощения
- `Audio/TierUpSound` (NEW, на Player в Game.unity): играет клип при ПОвышении тира. `_clip` СОЗНАТЕЛЬНО пуст — **владельцу подставить клип** в инспекторе. AudioSource создаётся кодом, группа микшера опциональна.
- `Collectables/AbsorptionFx` + `Collectables/WeightPopup` (NEW, на GO коллектора в Game.unity, ссылки в Collector): партиклы-бёрст в точке предмета (дефолтная система генерится кодом; можно подставить свой prefab в `_template`) + всплывающий «+масса» (3D TMP, летит вверх и тает). Цвета/размер/число частиц — в инспекторе.

### Конвейер пропсов
- `Assets/Editor/ItemPropFactory.cs` — меню **Mad Slime → Prop Factory**: указываешь папку с модельными префабами → для каждой модели генерится `D_<Model>.asset` (ItemDefinition: тир/масса с шагом) + `Item_<Model>.prefab` (модель + BoxCollider по рендер-баундам × padding + Item с проводкой) + опционально аппендится в пул выбранного `LevelTheme`. Уровень/масса/шаг/паддинг — в окне.

### Компиляция
- Прогнана в batchmode; правлены using-ошибки (Localization.cs, GrowthBarView.cs, LocalizedText.cs).

## Волна 4 (2026-09-11): язык из Яндекс SDK, лидерборд без авторизации, всё в конфиги

- **Локализация через Яндекс SDK**: `GetYandexLangMadSlime_js` читает `ysdk.environment.i18n.lang` → колбэк в `YandexLangBridge` (NEW, `Game/Localization/`) → `LocalizationService` применяет, ЕСЛИ пользователь ещё не выбирал язык сам (`SavesYG.Language == ""` — авто-режим; после нажатия кнопки в паузе выбор сохраняется и имеет приоритет). Незнакомые коды → английский. `YG2.lang` в плагине v2.0092 НЕ существует (это из неустановленного Localization-модуля) — поэтому свой мост.
- **Лидерборд без авторизации**: убраны auth-хинт/кнопка LOGIN из `LeaderboardMenu` (и из префаба). Аноним получает **сгенерированный ID** (`SavesYG.PlayerId`, генерится в `PlayerProgress.Awake`, формат `Player123456`), он уходит в Яндекс как `extraParam` при `setLeaderboardScore` и показывается в списке, если `publicName` пуст.
- **Всё через конфиги**: `PlayerConfig.asset` теперь центральный тюнинг игрока — `BaseMoveSpeed`, `RotationSpeed`, `MoveSmoothTime`, `MassPickupDivisor` (перевезён из serialized-поля PlayerTier), `AbsorptionDuration` (перевезён из Absorber). `PlayerTier` получает конфиг через `[Inject]`, `Absorber` — через serialized-ссылку на ассет (проводка в Game.unity). Радиусы детекторов и FX-параметры остаются в инспекторе сцены (они уже тюнятся там).
- Компиляция batchmode — чисто (0 ошибок, 0 проблем импорта).

## Волна 5 (2026-09-11): всё через Яндекс SDK + выглаживание до 10/10

- **PlayerId из Яндекс SDK**: `GetYandexPlayerIdMadSlime_js` → `ysdk.getPlayer({scopes:false}).getUniqueID()` (работает и для анонимов) → `YandexEnvironmentBridge.PlayerIdReceived` → `LocalizationService` пишет в сейв. До прихода ID из SDK действует временный `Guest-XXXXXX` (генерится в `PlayerProgress.Awake`, константа `GuestIdPrefix`) — SDK-шный ID его перезаписывает. ID уходит в лидерборд как `extraParam` (виден в списке для анонимов).
- **Мосты consolidated**: `YandexLangBridge` → `YandexEnvironmentBridge` (lang + playerId в одном приёмнике, GO «YandexLangBridge» на ProjectScope).
- **`AdsConfig` → `YandexConfig`** (файл, класс, ассет `Scriptables/AD/YandexConfig.asset`, меню «Mad Slime/Yandex Config»): теперь единый конфиг Яндекс-фич — `InterstitialEveryLevels`, `DoubleRewardId`, `NextLevelRewardId`, `LeaderboardName` (переехал из строк в LeaderboardReporter/LeaderboardMenu; оба теперь держат ссылку на ассет — проводка в Fill.unity и LeaderboardMenu.prefab).
- **DRY генерации**: новый `Game/Level/ZoneLayoutPlanner.cs` (plain-класс: FilterPool по тиру, ResolveSpacing, Collect Grid/CircleGrid/Circle/Scatter на System.Random) — `LevelGenerator` и `LayoutPreviewDrawer` больше не дублируют математику позиций. Превью детерминировано (seed `zoneIndex*7919+17`), генератор — time-seeded. Экстенты обводок в превью считаются из фактических позиций.
- **Мёртвое убрано**: `TierResolver.GetTierLabelFor` + `TierThreshold._label/Label` (заменены на ключи `tier_*` локализации), стартовые эмиты `TierChanged/MassChanged` в `PlayerTier.Awake` (все подписчики сами пуллят состояние в OnEnable/Start).
- `Absorber`: валидация конфига перенесена в Awake.
- Компиляция batchmode — чисто.

## Волна 6 (2026-09-11): список лейаутов + починка превью

- **`LevelConfig._layout` → `_layouts: List<LayoutSet>`**: каждый запуск уровня берёт СЛУЧАЙНЫЙ LayoutSet из списка (`LevelGenerator.PickLayout`, fail-fast: пустой список / пустой слот / лейаут без зон — исключение с внятным текстом). Оба конфига (Room/Table) мигрированы в YAML (в обоих пока лежит TableLayoutSet).
- **Превью (LayoutPreviewDrawer)**: рисует ВСЕ лейауты конфига сразу, метки зон с префиксом `L{индекс лейаута}`. «Уровень не менял отрисовку» было из-за того, что оба диапазона LevelsCatalog указывали на ОДИН TableLevelConfig — резолвер возвращал один и тот же лейаут. Чтобы уровни отличались — разные конфиги с разными лейаутами в каталоге.
- **LayoutPreviewDrawerEditor**: ручки перетаскивания центров зон теперь редактируют лейаут, выбранный полем «Handle Layout Index» в инспекторе drawer'а (гизмо рисует все, ручки — один).
- Каталог сейчас: 0–4 и 5–6 → TableLevelConfig (одинаковый). RoomLevelConfig есть, но в каталог не добавлен; RoomLayoutSet пустой (0 зон) — при попытке играть с ним генератор бросит исключение с подсказкой.

## Волна 7 (2026-09-11): Custom Layout в превью
- Владелец правил RoomLayoutSet, но превью не реагировало: RoomLayoutSet нигде не использован, а оба диапазона LevelsCatalog ведут на TableLevelConfig → Preview Level резолвил один и тот же лейаут.
- `LayoutPreviewDrawer`: новые поля **Custom Layout** + **Custom Theme** — если заданы, рисуется и ручками редактируется именно этот лейаут (пул для авто-расстояний — из Custom Theme, иначе из темы зарезолвенного конфига, иначе пустой). Каталог не нужен.
- `LayoutPreviewDrawerEditor`: ручки следуют той же логике — Custom Layout приоритетен, иначе каталог+Handle Layout Index.

## Передача сессии (2026-09-11, работа закоммичена)
Вся работа (волны 1–7) закоммичена и отправлена в origin. 2026-09-13: ветки `main`, `MadSlimeV2` и `otecGlinomes83/MadSlime` на GitHub синхронизированы — все указывают на `5fe28fa` (fast-forward, история линейная). На любом компе достаточно `git checkout MadSlimeV2 && git pull`. Локальная MadSlimeV2 в старом чекауте `~/Repos/Unity/Mad-Slime` может отставать — там сделать pull.

### Что осталось сделать владельцу (ничего из этого не делается кодом)
1. **Яндекс.Консоль**: создать лидерборд с id `max_level` (тип «максимальный») — иначе очки не пишутся (в консоль падает ошибка, нефатально).
2. **Подставить клип** звука роста: Game.unity → Player → TierUpSound → `_clip`.
3. **Прогнать WebGL-сборку** на Яндексе: реклама/лидерборд/язык из SDK живут только в билде; проверить стены (подойти к борту), звук тира, «+массу» при поглощении.
4. **Данные уровней**: RoomLevelConfig не в каталоге; RoomLayoutSet пуст (0 зон) — генератор на нём бросит исключение с подсказкой; оба диапазона каталога пока смотрят на TableLevelConfig. Заполни и разведи — тогда Preview Level и уровни начнут отличаться.
5. Турецкий перевод делал я — глянь глазами.

### Технический долг (не срочное, из ревью)
- asmdef/тестов нет — большой рефакторинг начинать с разделения сборок.
- UniTask без пина коммита в manifest.json.
- Локализации строк вне таблицы нет — новые UI-строки добавлять через Localization.asset + LocalizedText/`Localization.Get`.

## Волна 8 (2026-09-13): тиры без привязки к пропсам — система запекания

Схема (согласована с владельцем):
- **Tier Table** (SO, глобальный, лежит `Assets/Scriptables/Tiers/TierTable.asset`): тир → Scale + Mass + BadgeColor + ShortLabel (S/M/L/B). Единственный источник размеров/масс.
- **Layouts Library** (SO, глобальный, `Assets/Scriptables/Levels/LayoutsLibrary.asset`): общий список LayoutSet'ов для ВСЕХ локаций (из LevelConfig лейауты убраны). Prefill: TableLayoutSet + RoomLayoutSet.
- **Prop Set** (SO, на локацию): просто префабы единичного размера. `RoomProps` (пуст), `TableProps` (предзаполнен пулом из TableTheme). Внутри два блока: `_props` (руки) и `_variants` (генерит Bake).
- **Bake** (`Mad Slime → Prop Bake`): PropSet + TierTable → на каждую пару (префаб × тир) дефинишн `D_<Проп>_<Тир>.asset` в `Assets/Scriptables/Items/Baked/<PropSet>/` с иконкой из `Assets/Resources/Icons/<Имя>.png` (фототулза НЕ тронута: одна фотка на пропс, тир на тарелке — бейдж S/M/L/B цветной из Tier Table) + запись в `_variants`.
- **Рантайм**: случайный лейаут из библиотеки → каждому префабу локации назначается ОДИН тир на весь уровень (случайно в диапазоне LevelConfig Min/MaxTier, с гарантией покрытия каждого тира) → зоны отбирают по назначенному тиру → спавн варианта (скейл тира, масса тира) → квота по вариантам (иконка честного скейла… нет — иконка одна на пропс, бейдж показывает тир).
- `Item`: + `SetDefinition()` (вариант подменяется на спавне), `Initialize(position, scale)`; ItemDefinition не изменён (варианты — обычные дефинишны).
- LevelTheme: ItemPool удалён (тема = материал + текстура fill). Prop Factory: без тира/массы/темы — только юнит-префабы + дефолтный Small/1 паспорт.
- Превью: Preview Level удалён (лейауты глобальны), поля Library/TierTable/PropSet — заполнить на объекте drawer'а в сцене!

## Как проверить компиляцию самому (без редактора)
`~/Unity/Hub/Editor/2022.3.62f2/Editor/Unity -batchmode -quit -nographics -projectPath . -logFile /tmp/u.log`
Успех = строка «Exiting batchmode successfully» в логе. Только когда проект не открыт в редакторе (нет Temp/UnityLockfile).

## Волна 9 (2026-09-14): минус-скейл коллайдеров — была ПРОБЛЕМА В РАЗМЕРЕ, не в скейле

Плейтест после «переделал модели» снова завалил консоль «BoxCollider does not support negative scale or size»
(Icecream1/Egg/Watermelon). Прошлый фикс (abs в Item.Awake) был про scale корня — а тут было **отрицательное
ЗНАЧЕНИЕ m_Size у BoxCollider'а**: `m_Size: {x: 0.076, y: 0.212, z: -0.029}` у Item_Icecream1.prefab.

Корень зла — `ItemPropFactory.ComputeBounds`: размер переводился в локальные координаты через
`InverseTransformVector(world.size)`. Размер — НЕ направление: у моделей, повёрнутых на 90° по X
(юзер повернул их, чтобы стояли вертикально), обратный поворот положительного вектора даёт минус в компоненте.
Поэтому сломались только 3 из 40 пропсов — именно те, что стоят с поворотом.

Сделано:
- `ItemPropFactory.ComputeBounds` — экстенты теперь считаются канонично: |worldToLocalMatrix|·size
  (по модулю элементов матрицы). Поворот/зеркало/скейл обрабатываются точно. Дегенерат (≤0) → исключение.
- `Item.Awake` — fail-fast: `_collider == null` → InvalidOperationException (как у остальных полей).
- Исправлены сами 3 сгенерированных префаба (Item_Egg/Icecream1/Watermelon): минус-компоненты m_Size → по модулю.
  Центры не тронуты (им можно быть отрицательными). По всем остальным префабам проекта минусов в m_Size нет.
- Проверка: batchmode-компиляция чистая.

Если будешь перегенерировать пропсы через Prop Factory — теперь размеры всегда положительные.
