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

## Волна 10 (2026-09-14): LevelConfigResolver — неготовая локация не роняет уровень

Плейтест: уровень 8 → RoomLevelConfig (в каталоге диапазоны 0–4 Table / 5–6 Room; 7+ никуда не попадает,
резолвер берёт последний диапазон = Room). Владелец удалил RoomProps.asset → эксепшн «no PropSet assigned»
на ровном месте.

Решение — та же семантика, что у PickLayout: «контент не готов» ≠ ошибка программиста.
- `IsPlayable(config)`: не null + PropSet назначен + в PropSet есть запечённые варианты.
- Совпавший конфиг неиграбелен → LogWarning (с причиной: ассет удалён / нет PropSet / нет вариантов) +
  fallback на первый играбельный конфиг каталога. Не играбельно вообще ничего → InvalidOperationException.
- Данные каталога НЕ трогал (владелец правит его на рабочем компе). Дыра диапазона 7+ остаётся — код её
  переваривает (fallback на последний диапазон + играбельность).

Когда владелец зальёт RoomProps вариантами и добавит зоны в RoomLayoutSet — Room-локация заработает сама,
без правок кода.

## Волна 11 (2026-09-14): двойная система предметов — детектились «призраки» без определений

Симптом после починки резолвера: Collector спамит «no Definition» на предметах с именем ровно 'Icecream1'
и NRE в MoveChecker → Item.Tier.

Причина — два слоя системы предметов одновременно:
- **Старая фабрика** вешала Item+BoxCollider прямо на сырые модели ItemsFORFACTORY (и ставила слой 3 =
  Collectable). Новая фабрика (волна 8) оборачивает модель в Item_* (Item+BoxCollider на КОРНЕ), но старые
  компоненты на моделях остались.
- Маски детектора/MoveChecker ловят только слой 3 (Collectable). Корни обёрток были на слое 0 — детектор
  их не видел; детектились ЗАЛИПШИЕ коллайдеры моделей со старыми ссылками на определения (владелец те
  ассеты удалил) → «no Definition» + NRE.

Сделано:
- 40 модельных префабов ItemsFORFACTORY: сняты залипшие Item+BoxCollider (и m_Layer-оверрайды) — модели
  теперь чистая визуалка. Проверено: ссылок не осталось, PropSet'ы ссылаются только на обёртки.
- 40 обёрток Generated/Item_*: корень перенесён на слой Collectable (3) — единственный коллайдер предмета
  теперь виден и детектору, и MoveChecker'у.
- Prop Factory: ставит Collectable-слой при генерации; слоя нет → InvalidOperationException.
- Item.Tier/Mass: Definition == null → InvalidOperationException с внятным текстом вместо голого NRE.
- Batchmode-компиляция чистая.

## Волна 12 (2026-09-15): попап массы — цифры-частицы (атлас + Custom1.x), AbsorptionFx вырезан

Решение владельца: AbsorptionFx удалить, при поглощении показывать «+масса» ЧАСТИЦАМИ, настраиваемыми
в инспекторе (никакого кодогенерённого VFX).

Важные факты про Unity 2022.3 (проверено компилятором/строками DLL):
- `ParticleSystem.EmitParams.startFrame` и `ParticleSystem.Particle.startFrame` НЕ существуют
  (появились позже). Пер-партикл кадр в 2022.3 подаётся только через **Custom Vertex Stream**:
  `SetCustomParticleData(list, ParticleSystemCustomData.Custom1)`, а нарезку атласа делает шейдер.
- `ParticleSystemRenderer.activeVertexStreams` — нет; в 2022.3 это метод `GetActiveVertexStreams(List<...>)`.

Что сделано:
- `Assets/Fx/DigitParticle.shader` — unlit-прозрачный партикл-шейдер: кадр = Custom1.x, атлас 11 клеток.
- `Assets/Editor/DigitAtlasBake.cs` (Mad Slime → Bake Digit Atlas): рендерит 0–9 и '+' из TMP-шрифта
  в `Assets/Fx/DigitAtlas.png` (клетка 64–256px, по умолчанию 128 → 1408×128), NPOT=None, mipmaps off.
- `WeightPopup` переписан: пул инстансов `_template` (твой PS-префаб), Show(pos, mass) эмитит по частице
  на символ ('+' = кадр 10), кадр пишется в Custom1.x, расстояние между цифрами — `_digitSpacing`
  (держать ≈ Start Size). Автовалидация шаблона: Play On Awake off, Simulation Space = Local,
  TS-модуль выключен, в Renderer есть Custom1.x. Пул освобождается, когда particleCount == 0.
- `AbsorptionFx` удалён (класс, поле в Collector, компонент в Game.unity). Collector теперь требует
  _weightPopup (fail-fast).

Чек-лист владельцу (один раз):
1. Mad Slime → Bake Digit Atlas → выбрать шрифт → Bake.
2. Создать материал: шейдер MadSlime/DigitParticle, в _MainTex — DigitAtlas.png.
3. Создать PS-префаб: Play On Awake OFF, Emission rate 0, Simulation Space Local, Start Size ~0.5,
   Color over Lifetime — фейд, Renderer: Material = материал из п.2, Billboard, Custom Vertex Streams
   += Custom1.x.
4. Game → Collector → WeightPopup → _template = префаб из п.3. _digitSpacing ≈ Start Size.

## Волна 13 (2026-09-15): попап массы отложен владельцем — вырезан весь механизм

Владелец решил продумать позже: убраны партиклы при поглощении ПОЛНОСТЬЮ — WeightPopup, тулза
Bake Digit Atlas, шейдер DigitParticle, папка Assets/Fx, поля/компоненты из Collector и Game.unity.
Поглощение сейчас не даёт визуального отклика цифрами (только звук тира и сама анимация Absorber'а).

Если решение изменится — готовая реализация лежит в коммите 89be06f (ветки main/MadSlimeV2/
otecGlinomes83/MadSlime): вернуть через git cherry-pick / checkout нужных путей. Технические находки
той реализации (нет per-particle frame API в 2022.3 → Custom1.x + шейдер; Get/SetActiveVertexStreams —
методы) останутся верными и после возврата.

## Волна 14 (2026-09-15): фиксы из плейтеста + ускорение притяжения

Баги из лога:
1. Fill-сцена: LeaderboardReporter падал «YandexConfig is not assigned». Причина: в поле лежал
   **RewardConfig** (гуид b59c6a28...) — чужой тип, Unity резолвит как null. Тот же битый референс
   был и в LeaderboardMenu.prefab. Оба заменены на реальный YandexConfig (Assets/Scriptables/AD/,
   гуид 8523d704...).
2. SkillsConfig: 2 из 4 записей ссылались на удалённые ассеты (гуиды 0c64651c/1900ca26) — warning
   при каждом показе Level Reward Popup. Мёртвые строки удалены, остались LowAttract/MediumAttract.

Фича: притяжение скиллом ускоряется у центра.
- AttractConfig: + ApproachMultiplier (множитель скорости в самом центре, default 3),
  + ApproachPower (степень кривой: 1 линейно, 2 парабола, выше — «экспонента», default 2).
- AttractSkill.OnAttractableDetected: multiplier = 1 + (M-1) * (1 - d/R)^Power, где R — текущий
  радиус AttractableDetector'а (растёт с тиром). На краю радиуса скорость базовая, к центру растёт.
  Валидация M >= 1, Power > 0 в Awake с подсказками.

## Волна 15 (2026-09-15): коллайдеры пропсов — по баундсу минус врезка, без запаса

Жалоба: на больших предметах игрок останавливается, не дойдя до предмета. Причина: Prop Factory бил
бокс с запасом _colliderPadding = 1.15 (плюс радиус сферы MoveChecker'а у игрока).

Решение владельца: бокс по баундсу, ещё и чуть меньше — чтобы можно было «войти» в предмет на ~0.25.
- Prop Factory: `_colliderPadding` заменён на `_colliderInset = 0.25` (слайдер 0–1).
  Размер оси = bounds − 2×inset, пол — 50% исходной оси (мелкие пропсы юнит-размера без пола
  получили бы нулевые оси; на высоких тирах масштаб множит врезку честно).
- Все 40 существующих Item_* префабов пересчитаны в YAML: stored_size / 1.15 (восстановленный баундс)
  → та же формула. Центры не тронуты.

## Волна 16 (2026-09-15): ИДЕЯ владельца — проход сквозь недоступные предметы + рентген-визуал

Идея: игрок проходит СКВОЗЬ предметы тира выше текущего (сейчас они его блокируют), а сами предметы
пока игрок внутри/рядом — рисуются полупрозрачной «сеточкой» (screen-door X-ray). Анализ сделан,
имплементация НЕ начата. Ждут решения владельца (см. «Открытые вопросы» ниже).

### Факты из кода (проверено 2026-09-15)
- Физика игрока НЕ участвует в блокировке: Rigidbody в Scripts нет вообще, `Mover` двигает transform
  напрямую. Блокирует только явное решение `MoveChecker.IsAbleToMove`.
- `Movement/MoveChecker.cs:40-50`: SphereCast по Collectable; тир ≤ текущего → уже пропускается
  (return true), не-attractable → false, тир выше → false. **Блокирующий кейс ровно один — высокий тир.**
- Убрать блок → `IsAbleToMove` вырождается в always-true (в маске только Collectable, стены на Default).
  Тогда SphereCast мёртв. Компонент, возможно, оставить под будущие стены (слой стен в маску +
  return false для не-attractable хитов — см. открытый вопрос про стены, волна/журнал выше).
- **Коллайдеры предметов снимать НЕЛЬЗЯ**: `Detection/GenericOverlapDetector.cs:28` ищет через
  `Physics.OverlapSphereNonAlloc` — физика видит только коллайдеры. Без них ItemDetector
  (сбор) и AttractableDetector (скилл) ослепают. Альтернатива — дистанционный перебор пула —
  признана неоправданной (3 класса ради нулевой выгоды).
- Сбор/притяжение уже отфильтрованы по тиру (`Collector.cs:59`, `AttractSkill.cs:87`) — проход
  насквозь ничего не ломает; предмет, ставший доступным пока игрок внутри, соберётся сразу
  (радиус детектора больше капсулы).
- RP — **Built-in** (`GraphicsSettings.asset` m_CustomRenderPipeline: {fileID: 0}, URP-пакета нет).

### Предложенная реализация (эскиз)
- Шейдер: один общий ghost-материал — дизер 4×4 Bayer + `clip()` в ОДНОМ непрозрачном проходе
  (буквально «сеточка»: половина пикселей вырезается, игрок просвечивает). WebGL-дёшево, нет
  блендинга и сортировки прозрачности. Пропы разноцветные — единый ghost-вид читается как
  «ещё не физика». Альтернатива (дороже): Standard Transparent + fresnel rim.
- Свап материалов: `renderer.sharedMaterials` между оригиналами и ghost-материалами (кэш массивов
  при `Initialize`), без `renderer.material`-инстансов, дружелюбно батчингу. Рендереры собирать
  по детям обёртки (корень на Collectable, модель — дети).
- Состояние ghost — на `Item` рядом с Initialize/Collect/Shutdown, сброс в Initialize.

### Открытые вопросы владельцу (заданы 2026-09-15)
1. Триггер рентгена: **А** — перекрытие капсулы игрока с боксом предмета (буквально «пока
   пролезаю»), или **Б** — все недоступные предметы в радиусе ItemDetector (видно заранее, что
   не по зубам; склоняюсь к Б как более ценному геймплейно).
2. Судьба `MoveChecker`: удалить вместе с RequireComponent в `Mover`, или оставить остовом под
   будущие стены (тогда то же решение, что и открытый вопрос про стены — решать одним куском).
3. Связка с волной 15: врезка боксов 0.25 («войти в предмет») с проходом насквозь теряет смысл —
   впрок она не мешает, но если берём проход, пересчитывать ничего не нужно.

## Волна 17 (2026-09-15): ghost-«сеточка» при входе в предмет

Схема владельца: игрок НЕ свапает материалы сам — он только командует предмету «включи сетку»,
свап живёт внутри Item («ни один компонент игрока не может влиять не на игрока»). Детект входа —
свой OverlapSphere-поллинг на Player (как у коллектора), с командой на выход.

- `Assets/Shaders/GhostDither.shader` (NEW): opaque-проход (Queue=Geometry, ForwardBase),
  screen-space дизер 4×4 Bayer (бранчлесс bayer2-формула, без массивов — безопасно для WebGL2) +
  `clip(_Opacity - threshold)`. Пиксели ниже порога вырезаются → «сеточка», игрок просвечивает.
  Нет блендинга/сортировки. Параметры: `_Color`, `_Opacity` (0.7 = лёгкая сеточка, 0.5 = шахматка),
  `_DitherScale` (крупность). Half-lambert от главного directional + ambient. Fallback Off
  (ghost не кастит тень — осознанно).
- `Item.cs`: `+ _ghostMaterial` (fail-fast в Awake), кэш рендереров + оригинальные/ghost-сеты
  `sharedMaterials` (тумбл без аллокаций, без `.material`-инстансов), `SetGhost(bool)` с гардом
  `_isGhost`. Сбросы: `Initialize` (пул) и `Collect` (полёт в Absorber — обычным).
- `Collectables/ItemGhostToggler.cs` (NEW, на Player): каждый кадр OverlapSphereNonAlloc
  (радиус = capsule.radius + _margin — сам следует за ростом капсулы, LevelScaler мутирует её;
  центр = корень игрока), маска Collectable, буфер 64. Гейт `item.Tier > CurrentTier` — сетка
  только на недоступные (доступные собираются мгновенно). Список `_ghostItems`: свип «вышел из
  радиуса → SetGhost(false) + remove», потом «новые хиты → SetGhost(true) + add». 0 аллокаций.
  Событий выхода у GenericOverlapDetector нет — потому отдельный поллер, не наследник.
- MoveChecker (tier-гейт прохода) НЕ тронут — проход владелец делает сам (волна 16/17).

### Владельцу на проводку
1. Создать материал: шейдер MadSlime/GhostDither, тюнить _Opacity/_DitherScale.
2. На Player в Game.unity: Add Component → ItemGhostToggler; _tierHolder = PlayerTier,
   _playerCollider = капсула игрока, _layerMask = Collectable.
3. `_ghostMaterial` в Item-префабах — ляжет на прогон нового пайплайна (префабы сейчас снесены,
   пересобираются фабрикой); до проводки Item падает в Awake по fail-fast — осознанно.
4. Сетка и блокировка прохода независимы: пока MoveChecker не правлен, недоступные предметы
   СЕТКУ покажут, но не пустят внутрь.

## Волна 18 (2026-09-15): слияние фабрик — один пайплайн Prop Factory

`ItemPropFactory.cs` переписан в единый инструмент (бейк + фабрика + иконки), поля выходных папок — ВСЕ
указываются владельцем, ничего «Generated» по умолчанию не создаётся. Удалены: `PropBakeWindow.cs`,
`ItemIconGenerator/`, `IconGenerationSource.cs`, сцена `IconGeneration.unity` (в билде её не было).

**Поля окна** (персистятся в EditorPrefs, ключ `MadSlime.ItemPropFactory`): Models / Prefabs / Definitions /
Icons Folder, Prop Set, Tier Table, **Ghost Material** (новое требование волны 17: `Item.Awake` fail-fast без
`_ghostMaterial`), Collider Inset (0.25), Force Icons. Прогон = одна локация (Room-прогон, Table-прогон).

**Стадии Generate:** иконки из МОДЕЛЕЙ (не обёрток — нет edit-mode Awake) → дефинишны `D_<Item_<Model>>_<Tier>`
(пишутся _tier/_baseMass из TierTable + _icon) → обёртки get-or-create (коллайдер по баундсу с инсетом,
слой Collectable, `_definition`=Small-fallback, `_collider`, `_ghostMaterial`) → PropSet целиком (_props по
алфавиту + _variants). Превалидация с abort: пустые поля, 0 моделей, дубли имён, модель без Renderer'ов
(LogError, не warning — Item.Awake бы упал), пустой TierTable, нет слоя Collectable.

**Баг, которого не было в плане**: свежий PNG импортируется как textureType Default →
`LoadAssetAtPath<Sprite>` = null → `_icon` никогда не ложится (поэтому у 14 старых D_* `_icon: 0`).
Тулза теперь форсит `TextureImporter.textureType = Sprite` (в т.ч. на уже лежащих PNG — идемпотентно).

Факты на момент прогона: префабы Item_* и иконки Item_*.png снесены владельцем (первый прогон с нуля);
`TableProps.asset` держал мёртвые гуиды (перезапись PropSet целиком их сносит); Get-or-create по пути —
гуиды префабов/дефинишнов живут между прогонами. Обёртки-префабы в Models Folder скипаются
( TryGetComponent<Item> ). Ошибочные строки LevelGenerator/LevelConfigResolver переведены с «Prop Bake»
на «Prop Factory». Компиляция batchmode НЕ прогнана — проект открыт в редакторе (Temp/UnityLockfile);
редактор пересоберёт сам.

**Фикс иконок (тот же день, первый прогон владельца)**: первый прогон дал битые иконки — белый/чёрный фон,
засвет. Причина: НЕ логика рендера (не тронута), а КОНТЕКСТ — старый тул работал только в выделенной пустой
сцене IconGeneration.unity (дефолтное солнце + дефолтный ambient, в кадре только модель), а объединённая
тулза рендерила в текущую открытую сцену: камера захватывала пол/фон (белый = пол Game, чёрный = фон другой
сцены), солнце сцены поверх иконкового света давало засвет, alpha не оставалась 0 — фон был закрыт геометрией.
Фикс: Generate теперь открывает временную дефолтную сцену (NewSceneSetup.DefaultGameObjects — точная копия
окружения старой сцены: дефолтное солнце intensity 1 + дефолтный ambient, проверено по YAML снесённой сцены),
рендерит иконки там и восстанавливает прежний setup сцен (GetSceneManagerSetup/RestoreSceneManagerSetup).
Несохранённые сцены: Unity спросит сохранение; отмена = прерывание Generate до порчи ассетов.
Перегенерация битых PNG — только с Force Icons (существующие считаются рукодельными).

**Фикс 2 (бесконечный «Importing assets»)**: шторм импорта начинался ПОСЛЕ строки Done — виноваты финальные
глобальные `AssetDatabase.SaveAssets()`+`Refresh()` (флап всего грязного после генерации и смены сцен) и сама
авто-смена сцен (промпт сохранения → NewScene → Restore). Убрано ПОЛНОСТЬЮ: тулза больше не трогает сцены —
владелец сам открывает ПУСТУЮ сцену и жмёт Generate (как в старом workflow, HelpBox это говорит). Глобальный
флап заменён на `AssetDatabase.SaveAssetIfDirty(_propSet)` — префабы/дефинишны сохраняются при создании,
иконки импортируются одним `Refresh()` ДО дефинишнов. После Generate у Unity не остаётся очереди — шторма нет.

**Фикс 3 (иконки всё ещё с мусором в кадре)**: белый фон = пол открытой сцены, чёрный = тёмная геометрия,
засвет = солнце сцены — камера иконков рендерила ВСЁ в фрустуме. Сцены больше не переключаются, вместо этого
ИЗОЛЯЦИЯ ПО СЛОЮ: тулза берёт первый безымянный слой 9–31 (`ResolveFreeLayer`, fail-fast если нет), модель +
камера + свет кладутся на него, `cam.cullingMask = 1 << iconLayer` (камера видит ТОЛЬКО модель), `light
.cullingMask` туда же, `light.shadows = None` (чужие тени в кадр не попадают). SetLayerRecursively обязателен —
дети модели на своих слоях. Прозрачный фон гарантирован: за моделью больше ничего не рендерится, остаётся
clear-цвет (0,0,0,0). Работает в ЛЮБОЙ открытой сцене — пустая сцена не нужна. Освещение = мануальный свет
(45°/-30°, 1.2) + ambient открытой сцены. Перегенерация старых PNG — Force Icons ON один прогон.

**Инсет коллайдеров отменён владельцем** (2026-09-15, после волны 16/17 с проходом сквозь недоступные
предметы стал бессмысленным): поле Collider Inset и ApplyInset удалены, бокс генерится ровно по рендер-баундсу
(`ApplyCollider` — size = bounds.size). Кнопка Reapply Colliders удалена вовсе — коллайдеры пересчитываются
каждым Generate (get-or-update-проход). Осталась одна кнопка Generate.

**Блок прохода снят владельцем (волны 16/17 закрыты)**: `MoveChecker.IsAbleToMove` больше не гейтит по тиру —
предметы (IAttractable) ВСЕ проходимы, блокируются только не-attractable хиты (стены, слой Wall в маске) —
иначе игрок выпал бы с арены. Поле `_playerTier` из MoveChecker удалено (старая ссылка в Game.unity
игнорируется Unity, сцену править не нужно). Ghost-сетка (`ItemGhostToggler`) уже была тир-гейтованной:
включается только на предметы тира ВЫШЕ игрока (`ItemGhostToggler.cs:76`), не менялось.

**Фикс 4 (Fill: «RewardConfig is not assigned» в Rewarder)**: та же болезнь волны 14, зеркально — в
`Rewarder._config` (тип RewardConfig) в Fill.unity лежал гуид YandexConfig (8523d704…) → Unity резолвит как
null → fail-fast. Заменён на реальный `Assets/Scriptables/AD/RewardConfig.asset` (b59c6a28…). Остальные два
`_config` в Fill.unity (AdScheduler, LeaderboardReporter) — тип YandexConfig, гуид 8523d704 там корректен.

**Чек-лист владельцу**: два прогона Generate (Room: RoomFact → Items/… + RoomProps; Table: TableFact → … +
TableProps), Ghost Material = Assets/Shaders/GhostMaterial.mat, Force Icons ON пока PNG не перезаписаны.
После прогона проверить иконки в квоте. 14 старых `D_*.asset` в корне `Scriptables/Items/` — снести после
удачного прогона и ОК владельца (перед удалением сверить гуиды по сценам/префабам).

## Волна 19 (2026-09-15): расследование «после магазина притягиваются все мелкие объекты»

Симптом владельца: Game → магазин → потыкать скины (в т.ч. залоченные) → выход → старт уровня →
«все мелкие объекты притягиваются, будто я на всю карту размером».

### Вердикт: магазин НЕВИНОВЕН (проверено по всему коду)
- Клик по залоченному скину: `ShopPanel.OnItemClick` → `ViewSelected` (превью в Shop-сцене) → `TryUnlock`
  → `Balance < Price` → return. Ноль глобальных записей: ни сейвов, ни статиков. Клик по своему скину
  пишет только `SavesYG.SelectedSkinType` (косметика: SkinApplier инстансит визуальную модель; у скинов
  нет коллайдеров/скриптов — проверено по префабам SlimeSkin/PacmanSkin).
- Game-сцена при возврате грузится с диска: `PlayerTier._mass=6 → Small`, детекторы `_radius` 0.7/2
  (Game.unity) — идентично холодному старту. DontDestroyOnLoad в Scripts нет; DDOL только ProjectScope
  (PlayerProgress/LocalizationService/бриджи) — массы/радиусов не держат. Root-синглтоны VContainer
  (LevelProgress/LevelConfigResolver) — LevelProgress.Reset вызывается в каждом Generate.
- Радиус аттракции меняется ТОЛЬКО через TierChanged (масса), масса — только через PlayerTier.Add.

### Настоящая причина: данные уровня — снежный ком на ковре из предметов
- `TableLayoutSet` (единственный играбельный лейаут — RoomLayoutSet с 0 зонами PickLayout скипает):
  зона 1 = Scatter, center (0,0) = ТОЧКА СПАВНА, `_count: 1000`, `_radius: 100`, тир Small.
  Остальные зоны: гриды средних/крупных с центрами ±80 и spacing 16.3/13.3 — лейаут авторён под карту
  ~160×160.
- `LevelGenerator._mapSize` = 30×30 → `ClampToMap` сплющивает radius-100 scatter: ~26 мелких
  накрывают поле с шагом ~1.4, остальные ~974 СХЛОПЫВАЮТСЯ НА БОРТА (сплошные стены из мелких).
- Снежный ком: мелкий = +1 масса (divisor 4, floor 1) → 44 шт = Medium (радиус ×3), средний =
  BaseMass 500 → +125 → Large (×6) быстро, Boss (×15) у стены из мелких/на гридах средних.
  `GenericOverlapDetector`: 0.98 → 2.94 → 4.2 → 10.5; AttractableDetector до 30. На ковре с шагом
  1.4 это выглядит как «притягивается всё, я размером с карту». Воспроизводится на КАЖДОМ старте
  уровня, не только после магазина — холодный старт без магазина должен дать то же.

### Кандидаты на фикс (решение за владельцем, код не трогал)
1. Данные: зоны под фактическую карту (radius ≤ ~13, counts ~30–60) или поднять `_mapSize` сцены
   под авторские зоны.
2. Опционально guard в `LevelGenerator`: считать clamped-позиции и LogWarning «N из M за картой».
3. Тюнинг снежного кома отдельно: TierScalerConfig.RequiredMass (50/500/10000) vs +1 за мелкий.

## Волна 19 (2026-09-15): плавный ghost — НЕ реализовано, рецепт готов (дополнение к 17)

Вопрос владельца: «сеточка включается моментально — можно плавно?» Проверка кода: ДА, можно;
сейчас — НЕТ. `Item.SetGhost` (Item.cs:107) свапает `sharedMaterials` в один кадр, `_Opacity`
прибита к общему `GhostMaterial.mat`. Всё нужное для плавности в проекте УЖЕ есть:

- Per-renderer свойство без инстансов материала — паттерн `CubeSpawner.cs:43` (MaterialPropertyBlock
  → SetPropertyBlock). Анимировать `_Opacity` в блоке: 1.0 (сплошной ghost-шейдер, порог дизера
  max ≈ 0.94) ↔ 0.5 (сетка). Свап материала делать на «сплошном» кадре (1.0) — попа от подмены
  почти нет, дырки нарастают/зарастают плавно. Общий .mat не трогается, у каждого предмета своя
  плотность; при желании тем же блоком лерпить `_Color` от исходного цвета к призрачному.
- Драйвер анимации — канон `Absorber.AbsorbAsync`: UniTask while + SmoothStep + Yield + токен.
  Точка врезки: `SetGhost(bool)` остаётся краевым триггером, внутри стартует/отменяет fade-таск;
  `ItemGhostToggler` не меняется. Duration — сериал-поле или в PlayerConfig (рядом
  AbsorptionDuration).
- Нюансы: флип направления посреди анимации (отменять текущий fade), `Collect()` во время fade
  (Absorber забирает предмет — fade обязан отмениться), сброс property block в `Initialize`
  (переиспользование из пула — иначе призрачность протечёт в следующий спавн).
- Попутно проверено: проход сквозь предметы уже в коде — `MoveChecker.IsAbleToMove` теперь
  `return hit...TryGetComponent(out IAttractable)` (сквозь ЛЮБОЙ attractable, блок только
  не-attractable — готово под стены: слой стен в маску + те самые false). PlayerTier из него выпрошен.

## Волна 20 (2026-09-15): плавный ghost РЕАЛИЗОВАН (по рецепту волны 19)

Владелец: «плавно появлялась и исчезала у каждого предмета, лишнего не плети». Сделано ТОЛЬКО в
`Item.cs`, `ItemGhostToggler` не тронут:
- `_ghostFadeDuration` = 0.25 (SerializeField, fail-fast на ≤0). Target-плотность читается из
  `_ghostMaterial.GetFloat(_Opacity)` в Awake (сейчас в ассете 0.26) + guard `HasProperty` —
  тюнинг остаётся на материале, в коде ничего не захардкожено. Solid-якорь = 1.0 (порог дизера
  в шейдере max ≈ 0.94).
- `SetGhost(true)`: свап на ghost-материалы на «сплошном» кадре → fade `_Opacity` 1.0 → target
  через MaterialPropertyBlock (общий .mat не трогается, у каждого предмета своя плотность).
  `SetGhost(false)`: fade до 1.0 → на сплошном кадре возврат оригинальных материалов + сброс блока.
- Драйвер — `FadeOpacityAsync`: UniTaskVoid + SmoothStep (канон Absorber), отмена по `_fadeVersion`
  (int-поколение вместо CTS — 0 аллокаций): флип направления посреди анимации убивает старый fade,
  стартует новый от текущего `_currentOpacity`. Destroy-отмена — `GetCancellationTokenOnDestroy`.
- `Initialize`/`Collect` теперь зовут `ResetVisuals()` вместо `SetGhost(false)`: жёсткий сброс
  (версия++, блок снят, оригинальные материалы) — призрачность не протекает в следующий спавн из
  пула, `Collect()` посреди fade забирает предмет обычным.
- НЕ проверено компиляцией: редактор был открыт (Temp/UnityLockfile), batchmode невозможен —
  следующему агенту убедиться в отсутствии ошибок компиляции.

## Волна 21 (2026-09-15): ghost-fade переведён на конфиг (SO) + DOTween-эйзы

Владелец: «почему скорость на итеме? отдельный конфиг; и раз можно — дотвин с выбором ease».
Выяснено: DOTween УЖЕ в проекте (`Assets/Plugins/Demigiant/DOTween.dll`, в Scripts раньше не
использовался). Итог:
- `Scriptables/Items/GhostFadeConfig.cs` (NEW, namespace Items, меню «Mad Slime/Ghost Fade
  Config»): `_fadeDuration` (0.25) + `_ease` (`DG.Tweening.Ease`, default InOutSine) — dropdown
  эйзов в инспекторе без новых зависимостей.
- `Item.cs`: ручной UniTask-цикл и `_fadeVersion` СНЕСЕНЫ — теперь `DOTween.To(ReadOpacity,
  ApplyOpacity, target, config.FadeDuration).SetEase(config.Ease).SetTarget(this)
  .SetLink(gameObject, LinkBehaviour.KillOnDisable).OnComplete(OnFadeCompleted)`. Поле `_ghostFadeDuration`
  заменено на `_ghostFadeConfig` (fail-fast в Awake: null-конфиг + FadeDuration ≤ 0). Возврат
  оригинальных материалов — в `OnFadeCompleted` при `Approximately(_currentOpacity, Solid)`;
  флип направления — `DOTween.Kill(this)` (SetTarget обязателен: у DOTween.To цель сама не
  ставится). KillOnDisable гасит твины при Shutdown из пула.
- `ItemPropFactory.cs`: поле «Ghost Fade Config» рядом с Ghost Material — ObjectField,
  missing-чек, `WriteItemFields` пишет `_ghostFadeConfig`, состояние окна (GhostFadeConfigPath).
- АССЕТ ЕЩЁ НЕ СОЗДАН (создание .meta запрещено скиллами — GUID даст только импорт .cs в
  редакторе). Проводка владельцем: 1) фокус Unity (импорт+компиляция), 2) создать ассет
  (ПКМ → Create → Mad Slime → Ghost Fade Config), 3) фабрика: выбрать конфиг → Generate ×2
  (Room/Table). До проводки Item падает в Awake по fail-fast — осознанно (как в волне 17).
  Настройка скорости/эйза теперь ТОЛЬКО в ассете GhostFadeConfig; плотность сетки — по-прежнему
  `_Opacity` в GhostMaterial.mat.
- Компиляция владельцем: 1 ошибка — при переписывании Item.cs потерялся `SetDefinition` (зовал
  LevelGenerator.cs:265) — возвращён, паблик-поверхность Item снова 1-в-1 с до-переписной. Заодно
  убит мёртвый `LevelLabelUI._labelFormat` (CS0414: текст лейбла давно из `Localization.Get("level_label")`).

## Волна 22 (2026-09-15): диагностические логи [Diag] для бага «после магазина всё притягивается»

Владелец настаивает, что баг именно после магазина; добавлены логи для прогона (префикс `[Diag]`,
грепается, снести после диагноза). Компиляция НЕ прогнана — редактор был открыт (UnityLockfile).

- `PlayerTier.Awake`: масса + тир + defaultMass при старте сцены.
- `GenericOverlapDetector.OnEnable`: baseRadius × tierScale → итоговый radius (оба детектора).
- `GenericOverlapDetector.OnTierSourceChanged`: radius X -> Y (tier) — ловит снежный ком вживую.
- `LevelScaler.Awake`: collider radius/height/multiplier; `OnTierChanged`: targetMultiplier.
- `SkinApplier.ApplySelectedSkin`: выбранный скин + localScale модели (ловит кандидат «скин ×100»).
- `ItemGhostToggler.OnEnable`: capsule radius + margin; ghost ON/OFF с itemTier/playerTier
  (владелец видит прозрачность на предметах «больше игрока» — по волне 17 это дизайн:
  тир предмета ВЫШЕ игрока → сетка; лог покажет, тот ли тир в гейте).
- `SessionStateLogger.OnSceneLoaded`: + timeScale (ловит залипшую паузу плагина YG2).

Что смотреть в логе после возврата из магазина: PlayerTier.Awake mass=6 tier=Small; baseRadius
0.7/2 с tierScale 1.4; ghost ON при itemTier>playerTier. Если mass/tier иные — вот и канал
магазин→состояние. Вариант «игрок огромный» опровергается collider radius в LevelScaler.Awake.

## Волна 23 (2026-09-15):GhostFadeConfig создан + 14 Item-префабов пропатчены (диагноз подтверждён логами [Diag])

Логи владельца подтвердили: до/после магазина состояние ИДЕНТИЧНО (mass=6 Small, радиусы 0.98/2.8,
collider 0.5×1) — канал «магазин→игра» отсутствует. Настоящая поломка уровня:
- `_ghostFadeConfig` не был заполнен НИ У ОДНОГО из 14 Item_* (Table), ассет не существовал →
  `Item.Awake` fail-fast падал на каждом `Instantiate` → Initialize NRE (`ResetVisuals`, _renderers
  null) → `SpawnItems` прерывался на ПЕРВОМ предмете → уровень почти не спавнился; заспавненные
  обломки падали в `Collect()` → UniTask умирал до Absorber/ItemCollected → масса не росла,
  LevelProgress.Reset не вызывался (в логе Quota 0/0). «Прозрачные итемы» — SwapGhostMaterials
  успевал отработать до NRE на конфиге: сетка мгновенно, без фейда.
- Сделано: создан `Assets/Scriptables/Items/GhostFadeConfig.asset` (duration 0.25, ease int 3 =
  InOutSine при Linear=0-нумерации; владелец проверит ease в инспекторе), .meta сгенерил импорт
  редактора (guid ee25c91a9b0da4b94b8654ae2cfe4341). В 14 префабов Item_* (Table) после
  `_ghostMaterial` вставлено `_ghostFadeConfig: {fileID: 11400000, guid: ee25c91a..., type: 2}`
  (sed по одному вхождению на файл, проверено grep'ом — 14/14).
- После реимпорта префабов в редакторе ожидаемо: полный спавн уровня, рабочий сбор/масса, плавный
  ghost. Логи [Diag] остаются до подтверждения владельцем, потом снести.
- Не тронуто: warning «RoomProps has no baked variants» (данные владельца: Room пустой, фолбэк на
  Table легален, волна 10); «снежный ком» данных TableLayoutSet — см. волну 19.

## Волна 24 (2026-09-16): фикс паузного поедания + компаундинг радиуса + проб «дальней» детекции

Разбор лога владельца (уровень 9, 2845 предметов, после магазина) + полный статический проход цепочки
(префабы 14/14: корень scale 1, коллайдеры 0.02–0.22; TierTable единственный 13/30/60/100; иерархия
Player вся в (0,0,0) scale 1; SlimeSkin — только mesh+renderer, слой 7, scale 52.19 = компенсация
импорт-скейла; ItemPool/ZoneLayoutPlanner/Absorber/MoveChecker/LevelTransitor чисты; загрузка сцен
синхронная не-аддитивная). Скин, магазин и «загрузка до подготовки» — ОПРАВДАНЫ.

Доказано логом: сотни «absorb» при нуле «absorbed»/mass/tier, квота 0/12 → ItemCollected не
срабатывал ни разу. Торрент-окно без «Gameplay Start» = пауза (GameplaySessionHandler.Awake →
Pauser → timeScale 0): Update-системы (детекторы, ghost) работают, AbsorbAsync/Mover на
Time.deltaTime заморожены. Владелец подтвердил: в пасте не двигался; видимое «ем со всей карты» —
очередь зависших Collect() доигрывает разом после снятия паузы.

Неразгаданное: ghost-запрос радиуса 0.7 (capsule 0.5 + margin 0.2) ловил предметы на 10–107 юнитов —
по сериализованным данным максимум ~22 мировых юнита у Boss. Нужен рантайм-проб.

Правки:
- `GenericOverlapDetector`: `_baseRadius` переносён в `Awake` (раньше брался текущий в OnEnable —
  компаундинг ×1.4 на каждом цикле Disable/Enable, после ~15 циклов радиус = вся карта); `Update`
  гейтится на `Time.timeScale == 0f` (закрыто и YG-пауза — Pauser.Count через timeScale).
- `ItemGhostToggler`: тот же гейт в `Update`; ghost ON дополнен itemPos/itemScale/timeScale.
- `Collector`: absorb-лог дополнен пробом itemPos/itemScale/detectorPos/radius/timeScale.

Компиляция НЕ прогнана (редактор открыт, UnityLockfile). Логи [Diag] оставить до следующего прогона
владельца: проб либо покажет аномальные itemScale/позиции («дальняя» детекция), либо подтвердит,
что всё было только на паузе — тогда снести.

## Волна 25 (2026-09-16): корень «дальней» детекции — m_AutoSyncTransforms: 0 + спавн телепортом

Логи владельца после волны 24 (магазин → Game, уровень 9): ghost ON на 47–116 юнитов и burst «absorb»
на 33–95 юнитов при radius 0.98/0.7 и detectorPos=(0,0.5,0), timeScale=1. Радиус корректен (0.7×1.4,
компаундинг починен) — геометрически запрос не может вернуть эти коллайдеры. Единственное объяснение:
физический мир видит их не там, где трансформы. `DynamicsManager.asset`: m_AutoSyncTransforms: 0.

Механизм: ItemPool.Get → Instantiate(prefab, _root) — предмет активен в origin рута пула, коллайдер
регистрируется в физ-мире в origin; Initialize телепортирует трансформ (enabled=true — но-оп, коллайдер
уже включён, перерегистрации нет). Сцена стартует на паузе (Pauser.RequestPause в Awake → timeScale 0,
FixedUpdate/симуляции нет) — стейл живёт до первого ввода. Первый кадр после Begin: Update детекторов
раньше первого FixedUpdate → запрос по stale-миру, все ~2845 коллайдеров «стоят» в origin = на игроке.
Коллектор ест всё ≤ тира (мелочь летит с карты), ghost глушит всё > тира. Квота 15→14 — рандом
AssignTiers/PickLayout, не персистентность.

Магазин НЕ причина и скин оправдан (диаги идентичны в обеих сессиях). Чистый запуск по коду должен
воспроизводиться так же — владелец в сессии-1 в геймплей до магазина не заходил, сравнения нет.

Правка:
- `LevelGenerator.Generate`: `Physics.SyncTransforms()` после `SpawnItems` — один явный синк батчем
  после раскладки, вместо включения autoSyncTransforms глобально (2845 коллайдеров, запросы каждый кадр).

Открытое после фикса: ghost-запрос 0.7 мировых юнитов = capsule.radius(0.5, локальный) + margin без
учёта lossyScale игрока (52.19 → капсула ~26 мировых). При живой физике игрок блокируется крупными
предметами на своей капсуле — центр никогда не подойдёт ближе ~26 юнитов к их коллайдеру, гост не
сработает никогда. До фикса «работал» на stale-артефактах. Владелец подтвердил фикс: радиус запроса =
`capsule.radius * |lossyScale.x| + margin` (ItemGhostToggler.Update, диаг в OnEnable дополнен).

Fill-сцена: FlyingCube двигается трансформом — если там есть overlap-запросы, тот же класс стейла;
не проверялось (вне репро).

Компиляция НЕ прогнана (редактор открыт). Проверка владельца: рекомпиляция → чистый запуск с первым
вводом и цикл магазин→игра — вакуума быть не должно. [Diag]-логи держать до подтверждения.

## Волна 26 (2026-09-16): полное ревью Assets/Scripts + Assets/Editor (123 файла, ~9.7k строк)

Ревью по правилам AI_RULES, без правок. Механика почти чиста (греп: 1 var, 2 тернарника, 0 комментариев,
0 Find*/UnityEvent/лямбд-подписок, модификаторы на всех типах). Панч-лист по категориям — в чате сессии.
Ключевые находки не-стилевые:
1. MoveChecker.cs:32 — SphereCast радиусом `_playerCollider.radius` (0.5 локальный при lossyScale ~52)
   — тот же класс локальный/мировой юнит-баг, что починен в ItemGhostToggler (волна 25).
2. ShapeFillOrchestrator.StartFill → ShapeFiller.BuildShape: GridBuilder.Build() зовётся дважды на
   каждую заливку.
3. ShapeFiller.Fill/StopFill — флаг вместо отмены: старый FillAsync может пересечься с новым циклом
   на общем _fillIndex.
4. PlayerTier.Add — TierChanged летит на каждом пикапе (Small→Small), 4 подписчика гардируются сами.
5. Player.Awake (`_playerConfig == null → return`) — молчаливый early-return, нарушение fail-fast;
   там же GenericOverlapDetector.OnEnable, SkinApplier, SkillUnlocker, LevelLabelUI.
6. AttractableDetector молотит OverlapSphere каждый кадр при неактивном скилле.
7. ShopContent.cs — единственный файл вне канона: var+LINQ+лямбды+опечатка skinDuplikates.
8. Структура: Scriptables/Tiers и Scriptables/Tier (два каталога), Scripts/Shop → namespace Skins,
   Scriptables/Items → namespace Items; файл ShopItem.cs содержит класс SkinItem.
Не трогать: SavesYG (JSON-ключи), PlayerInputActions (автоген), JS-коллбеки YandexAdsBridge (jslib).

## Волна 26 (2026-09-16): AI_GUIDE.md — путеводитель по системам для ревью
Полный проход по всем 123 собственным .cs (5 параллельных читателей + ручная сверка проводки сцен/префабов гуидами). Результат — **AI_GUIDE.md** в корне: 16 системных разделов (скрипты / поток / связи / чек-лист ревью) + сквозные инварианты + хвосты. Назначение: брать систему целиком и идти по её файлам.

Новые факты, которых не было в журнале (проверено grep гуидов по .unity/.prefab):
- `MassUI.cs` не ссылается ни одна сцена/префаб — мёртвый или невозвращённый в HUD.
- `Wallet`-компонент лежит в Game.unity, но НЕ зарегистрирован в GameLifetimeScope → [Inject] в Game-сцене не выполнится; потребителей Wallet в Game по коду нет — рудимент проводки.
- `DeathMenu.prefab` — v1-остаток (внутри только UIButtonSound, фабриками не спавнится). `Test.unity` вне билда.
- SessionStateLogger — НЕ MonoBehaviour, а plain IStartable (RegisterEntryPoint), подписка на sceneLoaded вечная по дизайну.

Ревью-находки по коду (не фиксилось — зафиксированы в AI_GUIDE как цели):
- Fail-fast дыры: `Player.Awake` молчит на null PlayerConfig + не валидирует `_inputReader/_collector`; `GenericOverlapDetector` молча отключает tier-масштабирование; `SkinApplier`/`SkillUnlocker` — молчаливые early-return на null-progress; `SkillHandler`/`SkillInputBinder`/`MassUI`/`LevelRewardPopup`/`ShopItemViewFactory` без валидаций.
- Fill: сетка строится дважды за StartFill; `Sprite.Create`-утечка в PlaceGhost; рестарт заливки не снимает летящие кубы (ложный FillCompleted); кубы не деспавнятся никогда.
- `Timer.StartCount` на исчерпанном таймере → повторный Finished (Continue защищён, StartCount нет).
- `LocalizationService`: подписки на bridge в Awake, отписка в OnDisable → после re-enable ответы Яндекса уходят в никуда.
- `AdScheduler`: повторный ShowRewarded до закрытия первого перетирает pending-колбэки.
- `LevelScaler` не применяет стартовый тир > Small к масштабу (CameraFollow — применяет); `TierThreshold._speed=1` (забытая строка) валидна для Mover.
- `QuotaGenerator` при typesTarget=0 отдаёт пустую квоту → FillPercent навсегда 0, уровень непроходим молча.
- Магазин персистит МИМО PlayerProgress (напрямую YG2.saves) — асимметрия шва, осознанная.

Хвосты прежние: [Diag]-логи снести после подтверждения; компиляцию волн 20+ прогнать batchmode; лидерборд max_level / TierUpSound._clip / Room-данные / снежный ком — за владельцем (полный список — AI_GUIDE §19).
