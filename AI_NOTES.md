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

## Волна 27 (2026-09-16): идеи гейм-филя — поглощение и заливка формочки (без правок, только план)

Полный проход по цепочкам фидбека. Контекст текущего состояния:

- **Поглощение:** предмет летит в слайм по SmoothStep-лерпу позиции+скейла за фиксированные 0.3с (`Absorber.AbsorbAsync`). Фидбек после прилёта: CameraImpulse (pull/push оффсета), PlayerPickupSound (один клип + рандом pitch), тихое обновление квотного UI.
- **Заливка:** бордер появляется мгновенно одним кадром, кубы летят по прямой SmoothStep-лерпу из одной сериализованной точки, прилёт — резкий snap в identity без эффекта, звук на каждый куб без лимитера, при 100% ничего кроме Win-окна.
- **Факты по проекту:** партикл-систем — ноль ни в одном префабе; DOTween уже используется (Item.SetGhost); HUD-процента заливки в Fill-сцене нет (FillUIFabric спавнит только окна); SoundLimiter есть и свободен.

### Часть 1 — фил поглощения (сцена Game)

1. **Профиль втягивания** — `Absorber.AbsorbAsync`. SmoothStep тормозит и на старте, и в конце — для «вакуума» нужен ease-in (медленно отрывается, разгоняется в пасть). Плюс длительность от дистанции (`distance / absorbSpeed`, кламп сверху): сейчас дальний и ближний предметы прилетают одновременно — близкий сбор ощущается вязким, дальний — телепортом. Новый параметр в `PlayerConfig` (AbsorbSpeed / MaxAbsorbDuration).
2. **Спираль/вращение в полёте** — тот же `Absorber`: вращение предмета с разгоняющейся угловой скоростью + небольшой тангенциальный оффсет (квадратичная безье). Прямая линия в пасть читается «телепортом», спираль — «всасыванием».
3. **Squash&stretch слайма** — короткий punch-scale модели на каждый пикап. Обязательно через ДОЧЕРНИЙ трансформ модели: `_modelTransform` занят сглаживанием `LevelScaler`, их нельзя смешивать. На тир-ап — эластичный овершут: сейчас `LevelScaler.GrowAsync` через SmoothDamp затухает без единого отскока, рост «не объявляет» себя телом.
4. **Тир-ап момент** — сейчас только скейл+камера+звук. Добавить: расходящееся кольцо (меш/декаль под ногами), короткий хитстоп (~80мс timeScale 0.1 — проверить совместимость с YG-паузой/GameplayAPI, см. шаг 0 плана), FOV-kick камеры. Самая дешёвая большая эмоция в игре.
5. **Частицы** — puff на поглощение (брызги цвета предмета) + ring на тир-ап. В проекте их нет вообще, достаточно одной пуловой системы на эффект.
6. **Звук по массе** — `PlayerPickupSound`: громкость/клип от `definition.BaseMass`, на крупный предмет — второй слой «глоток», baseline pitch чуть растёт с тиром (звук прогресса).
7. **CameraImpulse** — сейчас только pull/push оффсета. Добавить микро-шейк на предметы выше порога массы; на тир-ап FOV-punch — на телефоне читается лучше, чем оффсет.
8. **UI-панч** — тарелка квоты в `QuotaUI` и `GrowthBarView` пунчатся при сборе квотного предмета. Сейчас обновление цифр тихое.
9. **Пассивный магнит (опционально)** — предметы в части радиуса детектора сами дрейфуют к игроку до формального захвата. КОНФЛИКТ: это ядро ценности `AttractSkill` — пассивка может съесть скилл. Решение владельца.

### Часть 2 — фил заполнения (сцена Fill)

1. **Landing pop** — на `FlyingCube.Arrived` скейл-пунш (1.15 → 1.0, DOTween уже есть) + лёгкая вспышка. Куб «шлёпается», а не «вклеивается».
2. **Полёт куба** — `FlyingCube`: дуга (рандомный боковой оффсет по безье) + вращение в полёте с плавным доворотом к identity в последних 20% пути (сейчас резкий snap) + stretch по вектору скорости. Один класс — три дешёвых улучшения.
3. **Звук прибытия** — `FlyingCubeArrivalSound` играет PlayOneShot на каждый куб БЕЗ SoundLimiter: при spawnInterval 0.04с и flightDuration 0.5с это ~25 звуков/сек — каша. Фикс: лимитер + pitch, растущий с процентом заливки (эффект «дожимания» к 100%).
4. **Порядок заливки** — `GridBuilder.SortFillTopToBottom` даёт «занавес сверху». Снизу вверх читается как «заливка жидкостью» — совпадает с фантазией слайма. Альтернатива: спираль от центра. Решение владельца.
5. **Цветной призрак** — `ShapeFiller.PlaceGhost` красит фон в серый `_ghostColor`. Если красить той же текстурой в реальных цветах на ~40% непрозрачности — «раскрашивание по номерам»: игрок заранее видит картину и получает удовлетворение от её проявления.
6. **Бордер каскадом** — `SpawnBorder` спавнит периметр мгновенно. Каскад scale 0→1 по порядку вдоль периметра (~0.5с) = «рисуется формочка», момент предвкушения перед дождём кубов.
7. **Процент-счётчик** — в Fill-сцене HUD-процента нет. Большой анимированный %, считающийся по факту прилёта кубов (`_arrivedCount` в `ShapeFiller` уже есть), с панчами на 25/50/75/100.
8. **Финал** — при 100%: общий пунш всей формы (scale-пружина парента), конфетти, лёгкий zoom-out камеры «посмотри, что построил», и только потом Win-окно. Сейчас кульминация уровня — тишина.

### Минимальный набор (макс. фил за мин. правки)

Профиль втягивания + спираль (п.1-2 ч.1), landing pop + полёт куба (п.1-2 ч.2), лимитер звука прибытия (п.3 ч.2), тир-ап хитстоп+кольцо (п.4 ч.1), цветной призрак (п.5 ч.2). Пять точек, три из них — правка одного метода.

### AI-план исполнения

- **Шаг 0 — решения владельца до старта:** (а) пассивный магнит — да/нет (конфликт с AttractSkill); (б) порядок заливки — снизу вверх / спираль / оставить как есть; (в) допустим ли хитстоп через timeScale рядом с YG-паузой (плагиновые GamePause/GameplayAPI читают nowInterAdv/nowRewardAdv, Pauser считает через timeScale — конфликтов по коду не видно, но подтвердить).
- **Шаг 1 — Absorber:** ease-in вместо SmoothStep, длительность от дистанции, вращение+тангенциальный оффсет. Новые поля в `PlayerConfig` — проставить значения в .asset (batchmode-компиляция их не проверяет).
- **Шаг 2 — тир-ап:** кольцо + FOV-kick (+ хитстоп, если одобрен); эластичный овершут на модель через дочерний трансформ.
- **Шаг 3 — звук:** PlayerPickupSound по массе/тиру; FlyingCubeArrivalSound через SoundLimiter + pitch по прогрессу заливки.
- **Шаг 4 — заливка:** FlyingCube (дуга/спин/доворот + landing pop), SpawnBorder каскадом, цветной призрак, новый FillProgressUI (счётчик % по событию CubeArrived, панчи на milestones).
- **Шаг 5 — частицы:** пуловые puff (Collector) + ring (TierChanged), настройки в Scriptables по канону проекта.
- **Шаг 6 — финал формы:** пунш парента формы + zoom-out камеры при FillCompleted == 1.0, до спавна Win-окна.
- **Шаг 7 — попутный долг тех же файлов (волна 26):** кубы Fill не деспавнятся никогда; рестарт не снимает летящие кубы (ложный FillCompleted); сетка строится дважды за StartFill; Sprite.Create-утечка в PlaceGhost. Править в тех же точках, где будут фил-правки.
- **Шаг 8 — валидация:** batchmode-компиляция; прогон в редакторе (сбор → тир-ап → заливка до 100% и до фейла); проверить звук под лимитером на большом уровне.

**Примечание:** правки Absorber/FlyingCube не должны трогать [Diag]-логи Collector/ItemGhostToggler (волны 24-26) — они держатся до подтверждения владельцем и сносятся отдельным шагом.

## Волна 27 (2026-09-16): рандомный поворот предметов по Y при спавне

- `Item.Initialize`: `transform.rotation = Quaternion.Euler(0, Random.Range(0..360), 0)` перед скейлом —
  каждый спавн из пула получает свежий поворот. Коллайдер-бокс вращается с корнем (ок), детекторы/квота —
  по позициям (ок). Игровой call-site один: `LevelGenerator.cs:267`.
- Тиры игрока в конфиг НЕ переносились — уже там: `Assets/Scriptables/Tier/TierScalerConfig.asset`
  (TierScalerConfig → List<TierThreshold>): `_requiredMass` / `_scaleMultiplier` (рост) / `_speed`
  (скорость) / `_cameraOffsetMultiplier` (отдаление камеры). Потребители: TierResolver → LevelScaler,
  Mover, CameraFollow. Кода не требовалось — владельцу отвечено путём.
- Делитель массы — `Assets/Scriptables/Player/PlayerConfig.asset` → `_massPickupDivisor` (сейчас 4),
  применяется в `PlayerTier.Add` ко ВСЕМ предметам (Round(mass/divisor), минимум 1). Отдельного
  делителя для не-квотовых предметов НЕТ; квота считается в `LevelProgress.RegisterCollected` по
  Definition и от делителя не зависит. Если владельцу нужен отдельный не-квотовый делитель — новое
  поле в PlayerConfig + ветка в Player.OnItemCollected/PlayerTier (не делал, не заказано).

## Волна 28 (2026-09-16): аттрактор пассивный, активных скиллов больше нет

Решение владельца: магнит всегда включён, маленький радиус (тюнинг базой `_radius` детектора в
сцене), тянет только tier ≤ текущего. Вся активная скилл-машерика снесена.

- `Collectables/ItemAttractor.cs` (NEW, бывший `Skills/AttractSkill.cs`): `git mv` с сохранением
  GUID `cbdb3ce3…` → сцена пережила переименование без правки m_Script. Наследование BaseSkill
  срезано: подписка `_detector.Detected` в OnEnable/OnDisable (свои, не виртуальные), тир-гейт и
  approach-кривая (force × (1+(M-1)·(1-d/R)^Power)) не тронуты. Поля `_config/_playerTier/_detector`
  сохранены → проводка в сцене выжила. Fail-fast валидации те же.
- `AttractConfig` — standalone SO (база SkillConfig срезана); ассеты Low/MediumAttractConfig живы
  (Medium сейчас не подключён никем — пресет для тюнинга).
- **Удалены**: `BaseSkill`, `SkillHandler`, `SkillInputBinder`, `SkillUnlocker`, `SkillTier`,
  `SkillConfig`, `SkillsConfig` (+ ассет), `LevelRewardPopup` (+ `PopupCanvas.prefab`), `_skillUnlocker`
  из `GameLifetimeScope` (поле/валидация/регистрация/using), `AttractPerformed` из `PlayerInputReader`,
  из `FillUIFabric` — `_skillsConfig`/`_levelRewardPopupPrefab`/`ShowLevelRewardPopup` и `[Inject]`
  PlayerProgress (использовался только попапом).
- **Сцены**: Game.unity — с GO Player снесены SkillHandler/SkillInputBinder/SkillUnlocker; GO
  «AttractSkill» переименован в «Attractor», с него снесён Timer, из компонента аттрактора убрано
  поле `_timer`. Fill.unity — из модификаций FillUIFabric убраны `_skillsConfig`/`_levelRewardPopupPrefab`.
- Радиус: тир-скейлинг детектора НЕ тронут (владелец: «работает как надо») — базу `_radius`
  (сейчас 2) владелец уменьшает в инспекторе сам.
- `ItemTier.cs` остался в `Skills/` (namespace Skills) — нужен всему домену; папка живёт с одним файлом.
- Пауза закрыта без правок: `GenericOverlapDetector.Update` гейтится на timeScale==0 — магнит замирает.
- Не тронуто: action «Attract» в `PlayerInputActions.inputactions` (мёртвый биндинг, автоген не
  правим — убрать может владелец в редакторе); `Skills.prefab` (SprintButton/AttractButton, v1-мусор,
  сценами не используется — кандидат на снос отдельно).
- Владельцу: подстроить `_radius` AttractableDetector в Game.unity; [Diag]-логи волн 22–26 всё ещё в коде.
- Компиляция batchmode — чисто (Unity на этой машине: `D:\3. files\Unity\Editor\2022.3.62f2`).

## Волна 29 (2026-09-16): мобильное управление — TouchJoystick (код готов, проводка за владельцем)

Контекст: владелец хочет играть с телефона (WebGL/Яндекс). GDD 1.2 прямо предусматривает
«телефон — джойстик, ПК — клавиатура». Решение владельца: писать свой компонент; ассеты Asset
Store (Joystick Pack и пр.) отклонены — старый Input API, чужой код вне канона, интеграцию всё
равно переписывать. Встроенный OnScreenStick (в Input System 1.14.2 есть режим
ExactPositionWithDynamicOrigin) тоже отклонён: один спрайт едет целиком (нет «круг+точка»), не
скрывается, зона появления — круг вокруг стартовой позиции.

- `PlayerInput/TouchJoystick.cs` (NEW): sealed, наследует `OnScreenControl` (создаёт виртуальный
  Gamepad, control path `<Gamepad>/leftStick`), IPointerDown/IDrag/IPointerUp. Схема: сам GO —
  полноэкранная невидимая зона (Image raycastTarget), дети — Background (круг) + Handle (точка,
  ребёнок фона). OnPointerDown — фон появляется в точке тапа
  (RectTransformUtility.ScreenPointToLocalPointInRectangle); OnDrag — ClampMagnitude по
  `_movementRange`, в стик уходит clamped/range; OnPointerUp/OnDisable — SendValueToControl(zero) +
  скрытие (в OnDisable база сама сбрасывает контрол в дефолт). Второй палец гейтится по pointerId.
  Гейт «сенсор ли это»: в Editor всегда true (тест мышью), в билде `Touchscreen.current != null`
  в момент нажатия — раз событие uGUI пришло от тача, устройство Input System уже создано.
- **YG2.envir НЕ существует**: в плагине v2.0092 `Modules/` содержит только Authorization+Storage;
  define `EnvirData_yg` включать НЕЛЬЗЯ (EventsYG2._GetEnvirData зовёт несуществующий
  YG2.GetEnvirData — компиляция сломается; та же причина, что с Adv/Leaderboard в волне 2).
  Проверено грепом плагина. Определение мобилы поэтому без YG2.
- Интеграция через стандартный путь: стик инжектится в виртуальный Gamepad → достаточно биндинга
  `<Gamepad>/leftStick` на действие Move; PlayerInputReader правок не требует, «старт по первому
  вводу» (MovementKeyPressed) сработает от стика автоматически.
- **Владельцу в редакторе** (автоген не правим руками):
  1. `Assets/Scripts/PlayerInput/PlayerInputActions.inputactions` → Move → Add Binding
     `<Gamepad>/leftStick` (инспектор сам перегенерит PlayerInputActions.cs).
  2. В UI.prefab под HUD — первого ребёнка «JoystickZone»: stretch fullscreen, Image (Alpha 0,
     raycastTarget on) + TouchJoystick; дети: Background (Image-круг) и Handle (Image-точка,
     ребёнок Background, anchored в центре). Драг рефов в `_background`/`_handle`;
     `_movementRange` ~100–140 (канвас-юниты). Зону держать ДО кнопочных канвасов по иерархии
     (raycast-приоритет кнопок остаётся выше).
  3. Спрайты круга/точки — любые круглые (временно сойдёт дефолтный UISprite).
- EventSystem в UI.prefab/Game.unity уже на InputSystemUIInputModule (проверено по GUID) — тачи в
  uGUI работают, Touchscreen на WebGL в Input System поддерживается.
- Не заказано: кнопки скиллов на мобиле (активных скиллов нет, волна 28).
- Компиляция batchmode не прогнана — проект открыт в редакторе (lock, exit 21). Проверить консоль
  редактора при фокусе или прогнать batchmode после закрытия.
- **Проводка владельца (факт по Game.unity) и фикс**: владелец собрал иначе — JoystickZone это
  отдельный Overlay-канвас под 3D-объектом HUD; TouchJoystick висел на голом ребёнке «Joystick»
  (100×100, БЕЗ Image → raycast туда не попадает, события не доходят); Handle был сестрой фона и
  вечно висел в центре (код прячет только Background, т.к. по схеме точка — ребёнок фона); Scale
  2.59 на Background. Дан чек-лист: Joystick → stretch fullscreen + Image (alpha 0, raycast on),
  Handle → внутрь Background, Background Scale=1 и size 260, Raycast Target off на фоне/точке,
  Sort Order = −1 на канвасе (ниже кнопок), movementRange 180–250 (CanvasScaler Constant Pixel
  Size → юниты = пиксели). Код не менялся.
- **Итог (пересборка с нуля, работает — подтверждено владельцем 2026-09-16)**: JoystickZone
  (Canvas Overlay, Sort Order −1) → Zone (stretch fullscreen, Image alpha 0 raycast ON, на нём
  TouchJoystick, movementRange 200) → Background (Image круг 260, raycast OFF) → Handle (Image
  точка 100, raycast OFF). Тап → круг в точке, ведение → движение, отпускание → скрытие.
  Осталось проверить на телефоне в WebGL-билде (гейт Touchscreen.current; мышью в билде не
  включается — задумано).

## Волна 30 (2026-09-16): срезаны спам-логи [Diag] при поглощении и ghost
- Убраны пер-событийные логи, засорявшие консоль в плейтесте: Collector (absorb/absorbed),
  ItemGhostToggler (ghost ON/OFF, вместе с неиспользуемым distance), PlayerTier.Add (mass X -> Y),
  GenericOverlapDetector.OnTierSourceChanged (radius X -> Y — стрелял на каждом +1 массы).
- Разовые [Diag] при Awake/OnEnable оставлены (Scaler, PlayerTier.Awake, baseRadius, capsule,
  SkinApplier) — спама не дают, диагностика осталась.
- Файлы: Collector.cs, ItemGhostToggler.cs, GenericOverlapDetector.cs, PlayerTier.cs. Логики не трогали.

## Волна 30 (2026-09-16): реформа массы/тиров — 2 конфига вместо 5 мест

Решения владельца: в SO только данные, логики нет; делитель массы снесён (единица массы одна: масса предмета = прибавка игроку); ItemDefinition = только иконка+тир; TierTable не переименовывался.

- **PlayerConfig** (+ `Scriptables/Player/PlayerTierThreshold.cs` NEW): к движению/всасыванию добавлены `_startMass` (бывший `_defaultMass` со сцены PlayerTier, был 6) и `_thresholds` (бывший TierScalerConfig, поля те же: _tier/_requiredMass/_scaleMultiplier/_speed/_cameraOffsetMultiplier). `_massPickupDivisor` удалён. Ассет перезаписан: пороги **0/50/4000/40000** — сохраняют прежний темп (средний с 50 мелких или 1 среднего; большой с 4 средних; босс с 4 боссов при табличных массах), scale/speed/camera перенесены как были. Мёртвое поле `_label` («Малый/Средний/…») ушло вместе со старым ассетом.
- **Снесено**: `Scriptables/Tier/` целиком (TierScalerConfig.cs+asset, TierThreshold.cs, обе папки) — гуид ассета 19460ac6… больше никем не нужен.
- **TierResolver**: `_config` = PlayerConfig (+fail-fast на пустой список порогов); API (`GetUnlockedTier/GetSpeedFor/GetTierProgress/GetScaleFor/GetCameraOffsetFor`) не менялся → LevelScaler/CameraFollow/GenericOverlapDetector/GrowthBarView не тронуты.
- **PlayerTier**: `_defaultMass` снесён, `_mass = _config.StartMass`, `Add` без делителя.
- **Player**: `[Inject]` + TierTable; `OnItemCollected` берёт массу `TierTable.Get(tier).Mass`. Молчаливый `if (_playerConfig == null) return;` заменён fail-fast (заодно валидация TierTable — кусок пунч-листа волны 26).
- **ItemDefinition**: `_baseMass` снесено — оно выпекалось из TierTable.Mass фабрикой и уже протухло (у всех Medium лежало 500 при табличных 1000; Large соответствовал). `Item.Mass` снесён; ItemPropFactory не печёт массу. Стало невозможно расхождение печи и таблицы.
- **CameraImpulse** (потребитель массы, пропущенный первым обыском): вместо `definition.BaseMass` — `TierTable.Get(tier).Mass`. ВАЖНО владельцу: кривая `MassToPullStrength` настроена на диапазон 0–50 (старый масштаб делителя) — при массах 1/1000/10000 всё не-Small упирается в потолок 2.5; кривую перенастроить в CameraImpulseConfig.asset.
- **Сцена Game.unity**: `TierResolver._config` переброшен на PlayerConfig.asset (гуид 5870f75c…) правкой YAML. ПРИ ФОКУСЕ РЕДАКТОРА Game не сохранять поверх — сначала перезагрузить сцену с диска.
- Данные TierTable не тронуты. Найдено: **Medium=Large=1000** — похоже на недосмотр (рантайм-старое было 500/1000); правится теперь в одном месте. badgeColor/shortLabel ЖИВЫЕ: бейдж тира (буква+цвет) на тарелках квоты, Plate.prefab → QuotaPlateUI.Setup. Цветные шарики превью уровня — LayoutPreviewDrawer.GetTierColor, захардкожено, к таблице отношения не имеет.
- [Diag] в Collector.cs владелец снёс в ходе волны; разовые логи PlayerTier.Awake сохранены.
- Компиляция batchmode НЕ прогнана — проект открыт в редакторе (lock, exit 21). Проверить консоль при фокусе.
- Точка отката: коммит f62d976 (запушит владелец — у сессии нет интерактивных учёток).

## Волна 31 (2026-09-16): свип нейминга по AI_RULES + фиксация правил
- Смертники исправлены:
  - `Saves/Saves.cs` → `SavesYG.cs`, `Scriptables/Skins/ShopItem.cs` → `SkinItem.cs` (git mv вместе с .meta, GUID целы — классы не тронуты: SavesYG = YG-конвенция, SkinItem = живые ссылки в коде).
  - `ShopContent.OnValidate`: `var skinDuplikates` → `IEnumerable<IGrouping<PlayerSkins, SkinItem>> duplicateGroups` (var под запретом + опечатка), лямбда-параметр `array` → `group`, добавлен `using Player;`.
  - Однобуквенные локалы: `LayoutPreviewDrawer` `int p` → `propIndex`, `float x/z` → `offsetX/offsetZ`; `ZoneLayoutPlanner` (CollectCircleGrid, CollectCircle) `float x/z` → `offsetX/offsetZ`.
  - Mutable static → `s_`: `Localization._table/_language` → `s_table/s_language`, `LayoutPreviewDrawer._zoneLabelStyle` → `s_zoneLabelStyle`.
- НЕ переименовано (контракты, зафиксировано в AI_RULES): поля `SavesYG` (`_openSkins`, `musicVolume`, `sfxVolume` — JSON-ключи сейвов), поля `LeaderboardPayload/LeaderboardEntry` (ключи Yandex-API под JsonUtility.FromJson), автоген `PlayerInputActions.cs`.
- AI_RULES.md дополнен: имя файла = имя класса; граница static-нейминга (`const`/`static readonly` = PascalCase-константы, mutable static = `s_`); раздел «имена под контрактом»; запрет однобуквенных распространён на `p`/`x`/`z`.
- Граница сглаживает противоречие в старых правилах (таблица `s_` vs пример `WaitForSeconds OneSecondWait`).
- Компиляция batchmode не прогнана — проект открыт в редакторе (lock, exit 21). Проверить консоль редактора при фокусе: ошибок CS быть не должно (правки — механические переименования).
- Хвост реформы: `Interfaces/IMassHolder.cs` удалён, `IAttractable` больше не наследует его — единственный смысл члена `Mass` жил на Item, которого больше нет. Потребителей IMassHolder вне IAttractable не было (проверено грепом).
- Хвост реформы 2: LevelScaler не применял ScaleMultiplier стартового тира (Small) — рост применялся только при смене тира, старт был жёстко 1.0 (пунч-лист волны 26). OnEnable теперь берёт GetScaleFor(CurrentTier), снапит множитель и применяет сразу (без SmoothDamp); скорость — так же от CurrentTier. Стартовый размер настраивается строкой Small в PlayerConfig.
- Хвост реформы 3: тир-бейдж с тарелок квоты снесён (владелец не заказывал). Удалено: объект TierBadge (+ RectTransform/CanvasRenderer/TMP) из Plate.prefab и его child-ссылка; поля _tierBadge/_tierTable из QuotaPlateUI (Setup теперь только иконка+счётчик); колонки _badgeColor/_shortLabel из TierEntry и TierTable.asset. Уникальные потребители ShortLabel/BadgeColor отсутствовали (проверено грепом). Вложения Plate в Game.unity/UI.prefab не содержат изменений удалённых компонентов.
- От владельца: фулл-палитру раскраски планирует на будущее, сейчас не заказано — ничего не строить.

## Волна 32 (2026-09-16): магнит — разгон от точки захвата + спираль

Жалоба владельца: «меняю цифры в AttractConfig — движение всё равно линейное». Диагноз (объяснён владельцу):
1. Путь магнита — прямая по построению (velocity всегда в центр), approach-ручки меняли только скорость вдоль неё.
2. Кривая разгона была размазана на весь радиус магнита (4×тир ≈ 5.6), а захват происходит на ItemDetector (0.5×тир ≈ 0.7): максимальный разгон жил внутри радиуса захвата, видимой оставалась часть ×1→×2.5 на ~5 юнитов — не читается.
3. «Линейный полёт», на который смотрел владелец — это Absorber (SmoothStep-лерп по прямой, 0.3с); его ручки не в AttractConfig. Орбита/спин/сквош абсорбера (план волны 27) НЕ реализованы — ждут одобрения.

Решение владельца: тянуть с ускорением от точки контакта + по орбите. Сделано:
- `AttractConfig`: + `_orbitStrength` (дефолт 0.5 — доля боковой скорости от радиальной; 0 = прямая). В ассете поля нет → возьмётся дефолт класса при реимпорте; тюнить в инспекторе.
- `ItemAttractor`: + `_collectDetector` (ItemDetector; fail-fast: null, attractRadius ≤ collectRadius, OrbitStrength < 0). Разгон нормирован на видимое кольцо: `approach = 1 − clamp01((d − collectR) / (attractR − collectR))` — при первом контакте скорость = Force, к захвату = Force×Multiplier, вся кривая видима. (Валидация радиусов — на серийных базах в Awake: оба детектора скейлятся одним тир-фактором, отношение стабильно.)
- Спираль: velocity = radial + tangent×OrbitStrength; tangent = (r.z, 0, −r.x) × сторона; сторона фиксирована на предмет (чётность instanceID — без состояний и аллокаций), путь не ломается на середине. Радиальная составляющая остаётся ровно Force×multiplier — предмет гарантированно приближается, спираль сверху.
- Game.unity: `ItemAttractor._collectDetector` → ItemDetector (&3196850747484158986).
- Компиляция НЕ прогнана — редактор открыт (lock). Новых API нет (Mathf/Vector3/GetInstanceID).
- Абсорбер (Безье-орбита + спин + сквош модели, волна 27) — по-прежнему не одобрен владельцем.

## Волна 33 (2026-09-16): «предметы респавнятся» — статический аудит + пробы в пул

Жалоба владельца: собранные предметы «респавнятся» посреди уровня, массы за них нет, тянутся к игроку снова.

**Статический аудит (полный, воскрешение через код невозможна):**
- Единственный `SetActive(true)` на предметы в проекте — `ItemPool.Get` (grep по всем Scripts). `Get` зовётся ТОЛЬКО из `LevelGenerator.SpawnItems`, который — только из `Awake` (один раз на загрузку сцены). Посреди уровня пул мёртв.
- Цепочка сбора герметична: `Collect()` выключает коллайдер сразу (летящий предмет невидим обоим детекторам — двойной сбор невозможен); `Shutdown` → `ItemCollected` → единственный Release-подписчик `LevelGenerator.OnItemCollected` → `Release` (инактив + в пул). `Player.OnItemCollected` даёт массу на каждый прилёт, пропусков нет.
- Сценарий «Get без Initialize» и «Release дважды» не воспроизводимы статически.

**Главный подозреваемый — двойники (данные, волна 19):** TableLayoutSet так и не перекроен владельцем: scatter 1000 clamped в 30×30 → уровень 9 = 2845 предметов (~14 типов → ~200 копий каждого). Спиральный магнит (волна 32) теперь НАГЛЯДНО тащит всё в радиусе — раньше копии стояли, теперь летят к игроку со всех сторон. «Съел бургер → через 3 юнита ещё бургер тянется» читается как респавн; массы «нет», потому что двойник ещё не собран. Спираль сделала существующий рой видимым — поэтому баг «появился» сегодня.

**Пробы добавлены (временные, [Diag], снести после диагноза):** `ItemPool.Get` — instanceID + reuse/new + pooledLeft; `ItemPool.Release` — instanceID + позиция. Сигнатура: `Get #X` посреди уровня ПОСЛЕ `Release #X` = настоящее воскресение (и лог покажет источник). Если Get'ов между Generate нет — двойники подтверждены, фикс = данные лейаута (чеклист волны 19: зоны под карту / counts 30–60), не код.

**Уточнение симптома владельцем (волна 33.1):** «подбираю S → перехожу на M → S-предметы больше не собираются и не дают массу; с каждым следующим тиром так же; собирается только текущий тир». Статически гейт `Collector: defTier > playerTier` такое запрещать не может. Гипотеза «висящая сфера сбора» (радиус сборщика ужат 0.7→0.3, центр на высоте 0.5×tierScale → до земли (0.5−r)×s; мелкие предметы проезжают под сферой) — числово не доказана (высоты коллайдеров предметов оценочные). Поставлены решающие логи: `Collector.OnItemDetected` — REJECTED/COLLECT с defTier/playerTier/радиусом/позициями; `ItemAttractor` — near-capture (предмет ближе 2×collectRadius, но не собирается). Один прогон S→M с касанием S-предмета назовёт ветку. Двойники объяснены владельцу: спавн-данные (1000-scatter), не клонирование.

**Диагноз ПОДТВЕРЖДЁН владельцем (волна 33.2):** ItemDetector `_radius` 0.3 → 0.7 — сбор починился. Механика: детектор сбора — ребёнок Player-рута в (0,0,0); рут поднят LevelScaler'ом на высоту капсулы = 0.5×tierScale; сфера центра = 0.5×s, досягаемость вниз = (0.5 − r)×s. При r=0.3 сфера висела над землёй (S: +0.4, M: +0.8, L: +1.2, Boss: +3.0); предметы ТЕКУЩЕГО тира (TierTable scale 13/40/70/100 — крупнее) дотягивались до неё, МЕЛКИЕ нижних тиров проезжали под сферой: магнит (AttractableDetector r=1.0 → досягаемость (0.5−1.0)×s < 0, землю достаёт) их таскал, сборщик не видел. «Собирается только текущий тир» = тир-ап поднимал сферу выше макушек нижних тиров. **Инвариант на будущее: у любого детектора, припаркованного в (0,0,0) на Player-руте, base radius ≥ 0.5 (базовый радиус капсулы), иначе на высоких тирах он отрывается от земли. Аттрактор при 1.0 — ок.** [Diag]-логи снесены (Collector/ItemAttractor/ItemPool).

**Хвосты волны 33 (за владельцем, не код):** массы 1/500/1000/10000 при порогах 50/4000/40000 → S-предмет после M даёт +1 из 4000 (полоска не шелохнётся — «не дают ничего» может быть балансом, не багом); количество предметов на уровень ~2845 (TableLayoutSet: 1000-scatter + 150 + 6–8 гридов) — при желании резать `_count` до ~500–700 суммарно.

## Волна 34 (2026-09-17): гейм-фил — абсорбер и заливка (одобренный список владельца)

Владелец одобрил из плана волны 27: Game 1,2,3,7,8; Fill — всё. Исполнено на базе после волн 27–33.

**Поглощение (Game):**
- `Absorber` переписан: ease-in (progress² — предмет медленно отрывается, разгоняется в пасть), длительность от дистанции (`distance/AbsorbSpeed`, кламп Min/Max), полёт по квадратичной безье с рандомной перпендикулярной дугой (Min/MaxArcFraction), спин вокруг рандомной оси (Min/MaxSpinSpeed), скейл предмета сжимается только в хвосте полёта (ShrinkStart=0.6 → конец). Конец безье = живая позиция игрока (предмет «догоняет», как раньше).
- `PlayerConfig`: вместо `_absorptionDuration` — 8 полей абсорбции (см. ассет, значения проставлены: speed 9, min/max duration 0.1/0.45, arc 0.1/0.3, spin 120/540, shrinkStart 0.6). Fail-fast перекрёстных значений (min>max) — в `Absorber.Awake`.
- `Player/ScalePunch.cs` (NEW, guid 1054b88a…): UniTask-пунш скейла таргета со стэкингом (быстрые пики суммируются до MaxStackedStrength). Висит на новом GO **SkinContainer** (ребёнок Model, fileID 900000000000000100-102) — контейнер вставлен МЕЖДУ Model и скином: `SkinApplier._skinsContainer` переброшен на него, `ScalePunch._target` = сам SkinContainer. Model остался у LevelScaler — конфликта нет. `Player._collectPunch` → ScalePunch; пунш на каждый пикап.
- `LevelScaler`: рост тира теперь easeOutBack (`_growDuration` 0.55, `_overshoot` 1.7) — эластичный овершут вместо затухающего SmoothDamp. Стартовый снап из волны 31 не тронут.
- `CameraImpulse`: два новых канала — Shake (масса предмета ≥ ShakeMassThreshold=10 → микрошейк) и FovKick (= TierFovKick=7 на тир-ап, экспоненциальное затухание). `CameraImpulseConfig` (+6 полей, ассет пропатчен). `CameraFollow` применяет: перлин-шейк по right/up (`_shakeFrequency` 25) + `fieldOfView = base + FovKick` (камера ищется TryGetComponent — CameraFollow обязан висеть на камере, fail-fast).
- `QuotaUI`/`GrowthBarView`: DOTween DOPunchScale на тарелку при изменении квоты / на бар при смене массы (поля _platePunchStrength/_barPunchStrength…).

**Заливка (Fill):**
- `FlyingCube` — FSM Idle/Growing/Flying/Settling: полёт по безье-дуге, ориентация LookRotation(velocity) + крен вокруг forward, в последних 25% плавный доворот в identity (снапа нет), stretch по оси скорости (пик в середине), прилёт — сквош-сеттл (xz+0.6p, y−p, затухание за _settleDuration 0.18). `GrowIn()` — рост 0→скейл для бордера. Новые поля в FlyingCube.prefab (0.35/0.18/0.18/0.22).
- `GridBuilder`: сортировка заливки перевёрнута — снизу вверх (SortFillBottomToTop), «заливка жидкостью».
- `ShapeFiller`: призрак цветной (tint white, `_ghostOpacity` 0.4 — картина проявляется кубами поверх); бордер каскадом (SpawnBorderCascadeAsync, `_borderCascadeDuration` 0.5, каждый куб GrowIn); `Fill` стартует с задержкой `_fillDelay` 0.55 (каскад успевает отрисоваться); `FillFraction` (arrived/target) наружу; фикс утечки Sprite.Create (старый спрайт Destroy); двойной Build в `ShapeFillOrchestrator.StartFill` убран.
- `FlyingCubeArrivalSound`: SoundLimiter (компонент добавлен на GO Shape, fileID 1948289417, maxConcurrent 6) + `_minInterval` 0.03 + pitch Lerp(0.9, 1.35, FillFraction) — «дожимание» к 100%. Раньше был PlayOneShot на каждый куб без лимитера (~25/с).
- `UI/HUD/FillProgressUI.cs` (NEW, guid b14f080f…): большой % по факту прилёта кубов, панчи на 25/50/75/100. Живёт в FillUI.prefab (новый GO FillProgress: свой Overlay-канвас sortingOrder 20 + CanvasScaler 1920×1080 + TMP LiberationSans, fileID 900000000000000200-206); `_shapeFiller` → ShapeFiller сцены через модификацию PrefabInstance в Fill.unity.
- `ShapeFill/FillFinale.cs` (NEW, guid 81a276d3…): на FillCompleted(≥1) — конфетти (40 кубов из CubeSpawner, цвета случайных fill-ячеек, разлёт+гравитация+спин, схлопывание и Destroy к 1.1с), DOPunchScale всей формы, FOV-zoom камеры +9 (остаётся — «отъезд и полюбоваться»). `FillSessionHandler._winDelay` 1.3с — Win-окно (timeScale 0) спавнится ПОСЛЕ финала; фейл — без задержки.

**Проводка (проверено грепом по гуидам):** Game.unity — SkinContainer×3 блоков, Player._collectPunch, LevelScaler _growDuration/_overshoot, CameraFollow _shakeFrequency; Fill.unity — SoundLimiter+FillFinale на Shape (+в m_Component), ключи ShapeFiller/ArrivalSound, override FillProgressUI._shapeFiller; FillUI.prefab — GO FillProgress; FlyingCube.prefab — 4 поля; PlayerConfig.asset — 8 полей абсорбции; CameraImpulseConfig.asset — 6 полей.

**ВНИМАНИЕ владельцу:** редактор был ОТКРЫТ во время правок (batchmode: exit 21, project already open) — перед фокусом убедиться, что Game/Fill/FillUI.prefab/FlyingCube.prefab/PlayerConfig.asset/CameraImpulseConfig.asset не в dirty-состоянии: при фокусе Unity подтянет с диска, но НЕ СОХРАНЯТЬ поверх старые копии из памяти (сцена/ассеты), при предложении «reload» — перезагрузить. Компиляция batchmode НЕ прогнана (lock) — консоль редактора при фокусе покажет ошибки CS, если есть.
- Тюнинг: Absorber (speed/arc/spin/shrink), CameraImpulseConfig (порог шейка 10, кривая MassToPullStrength всё ещё на старом масштабе 0–50 — см. волну 30), LevelScaler _overshoot, FlyingCube stretch/punch, FillFinale count/speed/fov.
- `PlayerTierThreshold.cs.meta` — untracked при закоммиченном .cs: класс сериализуется инлайн (гуид-ссылок нет, поломки не будет), но мета должна попасть в коммит — гигиена.

### Волна 34, дочистка: все ручки вынесены из кода (второй проход, тот же день)

По требованию владельца захардкоженные параметры переехали в существующие конфиг-носители (без новых SO):
- `FlyingCube.prefab` +6: _stretchSqueeze 0.5 (насколько x/y сжимаются при вытягивании z), _arcFraction 0.25 (дуга куба), _min/_maxRollSpeed 240/480 (крен), _rotationSettleStart 0.75 (с какой доли пути доворот в identity), _settleSquashSpread 0.6 (расхождение xz относительно сжатия y при прилёте). Fail-fast min/maxRollSpeed в Awake.
- `PlayerConfig` (+ассет) +1: _absorbEasePower 2 — степень ease-in всасывания (1 = линейно, 3 = резче разгон).
- `FillProgressUI` (FillUI.prefab) +3: _punchVibrato 8, _punchElasticity 0.4, _punchMilestones [25,50,75,100] (список int, панч на пересечение).
- `QuotaUI` +2 / `GrowthBarView` +2 / `FillFinale` +2: vibrato/elasticity каждого DOPunchScale.
- `FillFinale` (Fill.unity) +4: _confettiUpBiasMin/Max 0.6/1.4 (конус разлёта по вертикали), _confettiScaleFactor 0.7 (размер от ячейки), _confettiFadeFraction 0.25 (доля финала на схлопывание; 0 = без фейда). Fail-fast upBias min>max.
- `CameraFollow` (Game.unity) +1: _shakeAxes {1,1} — вес осей шейка right/up (0 = ось выключена).
- Остаток в коде — форма/рандомизация, не значения: ось спина абсорбера (рандом-сфера), smoothstep-профиль полёта куба, синус-профиль ScalePunch, перлин-источник шейка, горизонтальный разлёт конфетти (±1 XZ) и скорость вращения конфетти (±360°/с).
- Batchmode-компиляция снова не прошла (exit 21, редактор открыт) — проверка на фокусе редактора.

### Волна 34, дочистка 2: [Tooltip] на всех SO-конфигах (RU)

- Покрыты все 17 SO-классов + вложенные serializable-записи: PlayerConfig, PlayerTierThreshold, CameraImpulseConfig, TierTable/TierEntry, RewardConfig, YandexConfig, GhostFadeConfig, AttractConfig, ItemDefinition, SkinItem, ShopContent, LevelsCatalog, LevelRange, LevelConfig, LevelTheme, LayoutSet, SpawnZone, PropSet/PropVariant, LayoutsLibrary, LocalizationTable/LocaleEntry. 90 тултипов, RU, с единицами и направлением тюнинга.
- Семантика спорных полей проверена по коду перед подписью: DefaultCountDivisor = вес не-квотных предметов в FillPercent (LevelProgress); AutoSpacingFactor/ScatterDistanceFactor — шаг и мин-дистанция в ZoneLayoutPlanner; RewardConfig thresholds — сверены с Rewarder (WinFull>порога даёт base×percent; Lose≥порога даёт base/divisor).
- SpawnShape — enum: тултипы на значения enum инспектор не показывает, оставлен без атрибутов; тултип SpawnZone._shape перечисляет варианты.
- Атрибуты аддитивные, поведение не тронуто; значения ассетов не менялись (владелец уже подкрутил абсорбер: min/max 0.25/0.5, spin 520/1080, easePower 3).
- Ошибка по ходу: в FlyingCube.UpdateSettling/ApplyFlightStretch было умножение Vector3×Vector3 (CS0019, найдено владельцем) — исправлено на покомпонентное.
- Фикс владельца: процент заливки рисовался ПОВЕРХ Win/Fail меню (канвас FillProgress имел sortingOrder 20 против 0 у окон). Сделано: сортирующий порядок понижен до −10 (overlay всегда выше 3D-сцены, порядок лишь среди канвасов) + FillProgressUI подписан на ShapeFiller.FillCompleted и прячет себя (SetActive false) в момент завершения заливки — и на победе, и на фейле, и при пустой заливке.
- Фикс владельца: GrowthBarView дёргался пуншем скейла при каждом сборе (выглядел как «бар сам увеличивается» — Image растянут лейаутом, пунш скейла на нём ломает размер). Пунш снесён полностью (4 поля _barPunch* удалены); вместо него fillAmount плывёт к цели твином DOTween.To (OutQuad, `_fillSmoothDuration` 0.25, дефолт из инициализатора — YAML сцены не правил). Квотные тарелки (QuotaUI) пунш оставлен — это осознанный фидбек сбора, не бар.
- Фикс владельца: тарелки квоты ползли вверх и не возвращались — DOPunchScale, убитый посреди анимации, оставляет трансформ раздутым, следующий пунш стартует от него (DOKill не восстанавливает значение). Пунш снесён: поп переехал в QuotaPlateUI (PlayPop: снап к _baseScale, твин прогресса 0→1, scale = base×(1+strength×(1−t)) — всегда ровно в базу; поля _popStrength 0.12/_popDuration 0.18 на тарелке, дефолты из инициализаторов, Plate.prefab не правился). У QuotaUI поля _platePunch* удалены, using DG.Tweening убран.
- Фикс владельца: шейк камеры «пиздец как сильный» — причина двойная: порог 10 при массах TierTable 1/1000 = шейк на каждый не-Small (стэкался до потолка постоянно) + злые дефолты 0.35/0.8. Новые значения (код + CameraImpulseConfig.asset): threshold 1000 (только Large/Boss; Medium пока =1000 — тоже трясёт, починка данных за владельцем), strength 0.15, max 0.35, recover 7.
- **[Diag]-логи снесены полностью** (хвост волн 22–33 закрыт): PlayerTier (Awake + масс на каждый пикап), LevelScaler (Awake + тир), GenericOverlapDetector.OnEnable, ItemGhostToggler.OnEnable (метод удалён целиком — был только ради лога), SkinApplier (все 3). Греп «Diag» по Scripts пуст. У ItemGhostToggler.Update вычисление capsuleWorldRadius осталось (нужно для запроса). SessionStateLogger не тронут — это отдельный класс-журнал сессии, не [Diag].
- **Тултипы скриптов — ОТКАТ по решению владельца.** Владелец просил тултипы только для FillFinale (и SO); моё расширение на все скрипты (5 параллельных агентов, 66 файлов) — перегиб, остановлено и откачено: 12 файлов с tooltip-only изменениями — git checkout; 7 файлов с нашей незакоммиченной работой (Absorber, ItemAttractor, ItemGhostToggler, FlyingCube, GridBuilder, ShapeFiller, FlyingCubeArrivalSound) — построчное удаление [Tooltip]. Остались тултипы ТОЛЬКО в Scriptables (SO) и FillFinale.cs. Греп чист.

## Волна 35 (2026-09-17): границы игрока — кламп по баундам пола, MoveChecker снесён

Решение владельца: размер карты нигде не задаётся цифрами — источник правды = расставленный в сцене
пол. Свип-тест больше не нужен: предметы не блокируют движение, прозрачность у камеры делает
ItemGhostToggler своим рейкастом.

- `LevelGenerator`: `_mapSize` снесено (в сцене лежало 200×200 при дефолте 30×30 — цифры в любом
  случае разъезжались с реальностью); границы = `_floorRenderer.bounds` (мировой AABB пола),
  захват в `Generate` до ApplyTheme. `ClampToMap` предметов переведён в мировые координаты
  (TransformPoint → кламп XZ в bounds ± margin). Свойство `MapSize` → `FloorBounds` (null-safe,
  нужен гизмо drawer'а в edit-mode). `_floorRenderer` стал обязательным (был опциональным — только
  тема) + `_mover` — fail-fast в Awake.
- `Mover`: `[RequireComponent(CapsuleCollider)]`, `SetBounds(Bounds)` — LevelGenerator пушит в своём
  Awake; `Move` после шага клампит XZ в bounds ± текущий радиус капсулы (радиус скейлится
  LevelScaler'ом по тиру, читается живьём). Fail-fast: Move до SetBounds → исключение. Пер-осевой
  кламп даёт скольжение вдоль борта — старый блок глушил всю скорость, диагональ в стену стопорила.
- Снесено: `MoveChecker.cs` (+meta, guid 997c9472…), компонент с GO Player в Game.unity,
  `RequireComponent(MoveChecker)`. Факт, закрывший вопрос: все Item — IAttractable, ветка
  attractable в `IsAbleToMove` всегда возвращала true → SphereCast реально блокировал ТОЛЬКО борта.
  С клампом система не нужна целиком; Mask Collectable|Wall (264) ушла вместе с ней.
- Сцена Game.unity (гуиды): LevelGenerator (&1691083851) `_mapSize {200,200}` →
  `_mover: {fileID: 688403098498416583}`; из m_Component GO Player удалён &8234523066227841108;
  блок MoveChecker удалён. Гуид больше нигде в Assets не встречается (греп).
- `LayoutPreviewDrawer.DrawMapBounds` рисует мировой AABB пола (y = bounds.min.y) вместо границ из
  MapSize — TransformPoint drawer'а в границах больше не участвует.
- Слой Wall (8) и коллайдеры бортов остались в сцене, но никем не читаются (движение трансформом,
  детекторы маскируют только Collectable) — мёртвый груз, снос за владельцем.
- **Редактор был ОТКРЫТ при правках** (как в волне 34): Game.unity и MoveChecker.cs правлены на
  диске — при фокусе НЕ сохранять сцену поверх (сначала Reload), иначе пропадёт и проводка `_mover`,
  и снятие компонента. Компиляция batchmode не прогнана (lock) — консоль редактора при фокусе
  покажет ошибки CS, если есть.

## Волна 36 (2026-09-17): сквош сбора — только от предметов своего тира, перевёрнутая кривая, параметры в PlayerConfig

- `Player.OnItemCollected`: пунш сквоша только при `item.Definition.Tier == _playerTier.CurrentTier`
  (сравнение до `Add` — против тира на момент съедения); предметы ниже тира собираются молча.
  Жалоба владельца: босс-слайм дёргался от каждого мелкого предмета.
- `ScalePunch.Punch` — без параметров (множитель `1f` снесён за ненадобностью).
- Анимация перевёрнута и отдана владельцу в кривую (итерации: power-кривая → DOTween-пресет →
  AnimationCurve по просьбе владельца после «работает как говнище»). `ScalePunch` — один твин
  прогресса 0→1 (`SetEase(Ease.Linear)` обязателен, иначе двойное shaping) по
  `DOTween.To(ReadProgress, ApplyProgress, …)`, скейл = `base × SquashCurve.Evaluate(t)` (идиома
  Item.cs: Kill/SetTarget/SetLink KillOnDisable/именованный OnComplete; UniTask-цикл снесён).
  Повторный Punch во время анимации перезапускает проход.
- Новые поля `PlayerConfig` (Header "Collect Squash"): `_squashDuration` 0.2 и `_squashCurve`
  (дефолтные ключи (0,1) / (0.35,0.88) / (1,1)); Y кривой = множитель размера. Старые
  `_squashStrength`/`_squashEase`/`_squashMaxStackedStrength` снесены — финальный вид после
  трёх итераций. PlayerConfig.asset YAML не правлен, подхватит при фокусе.
- `PlayerPickupSound` — рандомный берст вместо одиночного попа: на каждый сбор 1..N попов
  (`PickupSoundMin/MaxPopCount` 1..3) со случайными паузами 0.02–0.06 с
  (`PickupSoundMin/MaxPopInterval`), питч каждого попа `Random.Range(PickupSoundMinPitch,
  PickupSoundMaxPitch)` (дефолт 0.95–1.15). Гейт между берстами `_pickupSoundMinInterval` 0.05
  (был `_minInterval` 0.1 на компоненте — поле снесено). Берсты через UniTask.Delay с linked-CTS
  (создаётся в OnEnable, Cancel+Dispose в OnDisable + destroy-token), SoundLimiter.TryPlay на
  каждый поп (нет слота — поп пропускается). Нюанс: pitch на общем AudioSource слегка сдвигает
  уже играющие OneShots берста (клипы короткие — незаметно). Провка: `_config` на
  PlayerPickupSound (Game.unity, GO &2432990947744818834) перетащить вручную.
- Проводка: `ScalePunch` получил `[SerializeField] PlayerConfig _config` (идиома Absorber); в сцене
  поле пустое → перетащить PlayerConfig.asset в `_config` на SkinContainer (Game.unity,
  GO &900000000000000100), иначе Awake кинет исключение. Старые сериализованные
  `_strength/_duration/_maxStackedStrength` — мёртвые ключи YAML, Unity снесёт при сохранении сцены.
- Редактор открыт (batchmode exit 21) — компиляция при фокусе; сцену поверх диска не сохранять до
  Reload.
- Дочистка тултипов FillFinale (жалоба владельца «навожусь — название поля дублируется»): 3
  поля-ссылки (_orchestrator/_spawner/_gridBuilder) получили тултипы; 5 пересказывающих название
  переписаны с направлением тюнинга (confettiCount, upBiasMax, shapePunchDuration, fovDuration —
  с предупреждением про Win Delay). Остальные 9 тултипов волны 34 не тронуты. У владельца в
  инспекторе мог быть старый compile — тултипы на диске были незакоммиченными (+27 строк к HEAD).
- **Диагноз «тултипов нет в инспекторе» (байт-уровень по Library/ScriptAssemblies/Assembly-CSharp.dll):**
  в свежесобранной dll (19:37) ЕСТЬ строки волны 35 (Drag a Mover…, positive XZ size) и fail-fast
  FillFinale, но НЕТ НИ ОДНОГО нашего тултипа — ни FillFinale (в т.ч. закоммиченных в 02:06 SO),
  ни LayoutSet («Зоны спавна…» пробована побайтово). Все 149 кириллических строк dll — текст YG2.
  Вывод: инкрементальный кэш компиляции (Library/Bee) держит протухший снапшот исходников для
  части файлов; реимпорт отдельных скриптов хэши не чинит. Фикс: закрыть Unity → удалить
  Library/Bee (32MB, регенерится) → открыть → полная перекомпиляция. Фолбэк: + Library/ScriptAssemblies,
  затем Reimport All.

- **Фикс применён (2026-09-17):** редактор закрыт владельцем; Library/Bee и Library/ScriptAssemblies
  снесены (PackageCache/ArtifactDB не тронуты — без reimport всего). При первом открытии Unity —
  полная перекомпиляция скриптов; тултипы должны появиться (наведение на ЛЕЙБЛ поля, не на значение).
- **Пикап-звук упрощён (решение владельца «одно поглощение — один звук»):** берст-механика
  (PopCount 1–3, PopInterval) снесена целиком из PlayerPickupSound (асинхронность/UniTask/CTS
  ушли вместе с ней) и PlayerConfig (+ассет: 4 поля удалены). Осталось: один поп на ItemCollected,
  рандомное кд между звуками Min/Max Interval (0.05/0.1, новый _pickupSoundMaxInterval), рандомный
  питч. Валидации min>max в Awake сохранены (interval, pitch).

## Волна 37 (2026-09-17): аудит Яндекс SDK — решение снести костыльный мост

Жалоба владельца «реклама/лидерборд нихуя не работает». Аудит фактов:

- **Плагин YG2 v2.0092** (последняя, GitHub JustPlay-Max/Unity-PluginYG-2) — модульный. Установлены:
  ядро + Storage v1.021 + Authorization v1.023 + платформа YandexGames v1.0091. НЕ установлены
  модули **Adv** (RewardedAdv_yg/InterstitialAdv_yg), **Leaderboard**, **Localization** — их кода в
  проекте нет, нативных `YG2.RewardedAdvShow/NewLeaderboardScore/GetLeaderboardEntries/lang` не существует.
- **Костыли волн 2–5** (решено снести): `Assets/Plugins/MadSlimeYandex.jslib`,
  `Game/Ads/YandexAdsBridge.cs`, `Game/Localization/YandexEnvironmentBridge.cs`, гостевой
  `PlayerId` в `PlayerProgress` (езда в лидерборд как extraData).
- **Подтверждённые баги моста**: (1) чтение extra из лидерборда по несуществующим полям
  `extraParams/extraParam` — в ответе SDK поле `extraData` → имена не показывались; (2) deprecated
  API `ysdk.getLeaderboards()` вместо `ysdk.leaderboards.getEntries/setScore`; (3) setScore доступен
  ТОЛЬКО авторизованным — схема «аноним пишет счёт + PlayerId в extra» не работает серверно в
  принципе; кнопки входа в игре нет (вырезана в волне 4); (4) нет GameplayStop/паузы на рекламе —
  требование Яндекса, модуль Adv делает сам; (5) интерстишл стреляется и сразу грузится сцена.
- **Решения владельца (спрошено явно)**: 1) снести мост, ставить официальные модули Adv + Leaderboard
  + Localization (Tools → YG2 → Version Control — клики владельца в редакторе); LocalizationTable
  игры (RU/EN/TR) остаётся, модуль даст `YG2.lang` и убьёт YandexEnvironmentBridge. 2) Кнопка входа
  в окне лидерборда (YG2.OpenAuthDialog, модуль уже стоит); аноним смотрит топ-10 без своей строки.
  3) Ники в настройках НЕ делаем — у вошедших publicName из Яндекса, у анонимов строки нет.
- **Чек-лист владельцу**: лидерборд `max_level` в консоли (тип «максимальный», ОПУБЛИКОВАН — не
  черновик); rewarded-блоки с id = DoubleReward/NextLevel как в YandexConfig; реклама живёт только
  в сборке на домене Яндекса. После установки модулей — миграция AdScheduler/LeaderboardReporter/
  LeaderboardMenu на нативный API и снос моста одним куском (сначала модули, потом снос —
  поэтапно игру без рекламы не оставлять).

## Волна 38 (2026-09-17): мост снесён, всё на нативном API YG2

Модули владельцем установлены: InterstitialAdv v1.02, RewardedAdv v1.011, Leaderboards v1.01,
Localization v1.02 (дефайны InterstitialAdv_yg/RewardedAdv_yg/Leaderboards_yg/Localization_yg
прописались сами). Миграция:

- **Снесено**: `Assets/Plugins/MadSlimeYandex.jslib`, `Game/Ads/YandexAdsBridge.cs` (+ папка Ads),
  `Game/Localization/YandexEnvironmentBridge.cs`, гостевой `PlayerId` (SavesYG.PlayerId,
  PlayerProgress.PlayerId/GuestIdPrefix/Awake; JSON-ключ в старых сейвах остаётся, JsonUtility его
  игнорирует).
- **AdScheduler**: Setup(bridge) снесён; rewarded через `YG2.onOpenRewardedAdv/onRewardAdv(string)/
  onCloseRewardedAdv/onErrorRewardedAdv` (семантика прежняя: награда только при колбэке, грейнт
  после закрытия); интерстишл — `YG2.InterstitialAdvShow()` (у плагина свой кулдаун interAdvInterval
  60с + гард nowAdsShow внутри; TryShowInterstitial оставлен — зовётся FillUIFabric при рестарте
  после провала). Пауза/EventSystem/GameplayStop на время рекламы — ВНУТРИ плагина
  (autoPauseGame: 1, PauseGameYG сохраняет/восстанавливает timeScale, корректно с паузой меню).
- **LeaderboardReporter.Report(int)**: `YG2.SetLeaderboard(name, score)`; аноним отсекается явно
  (лог) + гардом плагина `player.auth`. SetLeaderboard у плагина молчит при enable=false.
- **LeaderboardMenu**: `YG2.GetLeaderboard(name, 10, 1, "nonePhoto")` → `YG2.onGetLeaderboard(LBData)`
  (фильтр по technoName, NO_DATA → leaderboard_empty, своя строка по uniqueID == YG2.player.id
  жирным, добстрока из currentPlayer если вошёл и вне топа). Кнопка ВОЙТИ (AuthButton в префабе,
  fileID 900000000000000204) видна при `YG2.player.auth == false` → `YG2.OpenAuthDialog()`;
  обновление по `YG2.onGetSDKData` (плагин сам перечитывает игрока после логина) + ре-запрос
  лидерборда. Лейбл через LocalizedText/_key auth_login (новый ключ Localization.asset: ВОЙТИ/LOGIN/GİRİŞ).
- **LocalizationService**: мост языка снесён — `YG2.lang` (модуль Localization, setLanguageMod =
  EveryGameLaunch по дефолту) + `YG2.onSwitchLang`/`YG2.onGetSDKData`. syncInitSDK: 0 → сейвы
  приезжают позже Awake, поэтому язык переприменяется в OnSDKData. Фикс семантики волны 4:
  авто-применённый платформенный язык больше НЕ пишется в сейв (_suppressPersist) — авто-режим
  живёт до реального ручного выбора.
- **Префаб LeaderboardMenu**: добавлены AuthButton (Image UISprite оранжевый + Button + текст TMP
  шрифтом Entries-ассета 616e1cb2 — кириллица) под списком, _authButton в компонент, кнопка
  добавлена в UIButtonSound._buttons.
- Не тронуто: YandexConfig (все 4 поля живы), Fill.unity (компоненты AdScheduler/LeaderboardReporter
  с _config остались как были), LocalizationTable/LocalizedText.
- **Не проверено компиляцией**: редактор был открыт (Temp/UnityLockfile). При фокусе редактора —
  перекомпиляция; проверить: нет ошибок, LeaderboardMenu.prefab не missing-script, кнопка видна
  (аноним) / скрыта (после ВОЙТИ в редакторе — симулируется мгновенно).
- **Редактору-владельцу (клики, опционально)**: Tools → YG2 → Settings → Rewarded Adv → включить
  «Skip the next interstitial after reward» (после rewarded не дёргать интерстишл); → Simulation →
  Leaderboards → добавить сим-запись с technoName = max_level для игры в редакторе (без неё в
  редакторе список «нет данных» — это норма).

## Волна 39 (2026-09-17): аудит UI — «New Text» на старте, видимость HUD, иерархия канвасов, концепт MainMenu (ТОЛЬКО отчёт, правок нет)

Полный проход по UI: 3 параллельных агента разобрали Game.unity (вся uGUI-иерархия с fileID),
Fill.unity + FillUI.prefab + UI.prefab + Plate.prefab, оконные префабы + Shop.prefab/Shop.unity.
Карта проводок — в транскрипте сессии; ниже — суть. **Решения владельца отложены до завтра**
(вопросы: скоуп A/B/C + снос мёртвого).

### Находки: заглушки/дефолты
1. **Текст таймера «numpers» (Game.unity, TMP 7349393060338764257) — «New Text» ВИДЕН от загрузки
   сцены до первого ввода**: TimerUI пишет только по Timer.Ticked, а Ticked стартует после
   Begin (первый ввод). Корень: у Timer нет наружного Remaining. Фикс: Timer + public float Remaining
   (fail-fast до Setup), TimerUI + Start() с рендером Remaining (канон GrowthBarView.Start).
2. LevelBar («New Text») и GrothTierText («New Text») — перезаписываются в 1-м кадре
   (LevelLabelUI.OnEnable / GrowthBarView.Start), не видны, но дефолты в YAML мусорные.
3. **LeaderboardMenu: заголовок «Leaderboard!» — статичная заглушка, никем не пишется, не
   локализована** (код пишет только entries). После волны 38 в префабе появился AuthButton —
   заголовок при этом не трогался (проверить при фикс-волне). Фикс: LocalizedText + ключ
   leaderboard_title.
4. Shop.prefab: баланс «999999» мигает до SDK-колбэка (Initialize перезапишет).
5. Win/Fail/PauseMenu/Plate/FillProgress — чисто (Initialize/LocalizedText до первого рендера).

### Находки: видимость и иерархия
6. Весь HUD виден с кадра 0 во время пре-старт паузы (GameplaySessionHandler.Awake → RequestPause).
   Требование владельца: таймер-бар и бар роста — скрыты до GameStarted. Квота+лейбл уровня
   предлагались как «превью цели» (остаются видимыми) — подтвердить.
7. Game: **8 overlay-канвасов** (Joystick, PauseButton, LeadserboardButton(опечатка), ShopButton,
   Quota, GameTimer, LevelLabelUI(свой канвас), GrothBarCanvas) под GO «UI» (Transform).
   Зоопарк скейлеров: ConstantPixelSize (Joystick/LevelLabel/GrothBar) vs SWS 1920×1080 (остальные)
   → элементы разъезжаются по разрешениям. CanBeOff (только лидерборд+шоп) скрывается по
   GameStarted, PauseButtonCanvas — вне его (пауза живёт в игре — ок). GrothBarCanvas вне ветки HUD.
   Весь UI инлайн в сцене.
8. Fill: FillUI.prefab инстанс (PauseButtonCanvas order 0, FillProgress order −10) + EventSystem
   сцены. FillProgress «0%» ставится в Awake — ок. Scale 0/0/0 у канвасных RectTransform'ов в YAML
   (FillProgress, PauseButtonCanvas) — не баг: Overlay-канвас сам контролирует свой RectTransform
   в рантайме (владелец видел процент в плее — волна 34), YAML-шум.

### Находки: битое/мёртвое
9. **FillUIFabric._yandexConfig == null в рантайме**: поле позже кода — в FillUI.prefab его нет,
   Fill.unity оверрайдит все поля кроме него → NRE на кнопке «следующий уровень за рекламу»
   после фейла (`_yandexConfig.NextLevelRewardId`). Awake валидирует 2 из 8 полей. Волна 38
   YandexConfig/FillUIFabric не трогала — finding жив.
10. **Мёртвое (0 ссылок, гуиды перепроверены)**: UI.prefab (дубликат инлайн-HUD Game.unity; внутри
    протухшие поля _deathMenuPrefab/_hudCanvas и «New Text»), DeathMenu.prefab (+missing script
    69a3c254…), Skills.prefab, LevelButton.prefab (+missing script e8fee9b3…), PauseButtonCanvas.prefab,
    **MassUI.cs** (ни сцена ни префаб не ссылаются), Test.unity (вне билда, отдельно спросить).
11. Мусор: Win/Fail префабы несут сериализованное `_pauser: {fileID: 0}` (старая версия, игнорируется);
    CanvasScaler окон Win/Fail — дефолтный 800×600 ConstantPixelSize (не скейлятся под телефон),
    у PauseMenu/Leaderboard — 1080×1920 SWS. Разнобой.

### Предложения (текстом владельцу, не начаты)
- **Волна A** — дефолты+видимость: Timer.Remaining + TimerUI.Start-рендер; GameTimerCanvas +
  GrothBarCanvas стартуют inactive, GameplayUIFabric показывает по GameStarted (уже слушает
  GameStarted ради CanBeOff); дефолты «0.0»/«Level 1»/«S» в YAML; leaderboard_title; проводка
  _yandexConfig в Fill.unity + fail-fast всех полей FillUIFabric.Awake; снос мёртвого (п.10).
- **Волна B** — реформа иерархии Game: один UICanvas (SWS 1920×1080) с группами-детьми
  JoystickZone (первый = низший приоритет рейкаста) / Navigation (шоп+лидерборд, скрывается по
  GameStarted) / SessionHUD (таймер+бар роста, inactive до GameStarted) / HUD (квота+лейбл+пауза).
  8 канвасов → 1, один скейлер. YAML-правка сцены по гуидам + перепроводка GameplayUIFabric/
  TimerUI/DI (GameLifetimeScope._quotaUI/_levelLabelUI). UI.prefab — снос (опция: пересобрать префабом).
- **Волна C** — MainMenu: сцена Menu первой в билде; MenuLifetimeScope (регистрация только
  LevelLabelUI — PlayerProgress из Root); Play → LoadGame; Shop → LoadShop (возврат через
  PreviousScene работает из коробки); Leaderboard → спавн LeaderboardMenu (нужен Pauser в сцене
  меню); Settings = AudioSettingsPanel + LanguageSwitcher прямо на панели; «Уровень N» =
  LevelLabelUI. LevelTransitor: + _menuScene/LoadMenu; кнопка «домик» в PauseMenu (опциональный
  action по паттерну _restartButton). Win/Fail не трогать — гринд непрерывный. YG2
  GameplayStart/Stop и интерстишл — без изменений.

### Открытые вопросы владельцу (задать завтра по требованию)
1. Скоуп: только A / A+B (рекомендовано) / A+B+C / пока ничего.
2. Снос мёртвого (п.10): всё / только MassUI.cs / оставить. Test.unity — тоже сносить?
3. Квота+лейбл уровня до старта — оставить видимыми (превью цели) или скрыть вместе с сессией?
4. Где ещё давать «в меню» в волне C: только пауза / пауза+фейл / после победы каждые N уровней?
