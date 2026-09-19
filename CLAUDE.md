# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working in this repository.

---

## ⚠️ ПЕРЕД КАЖДЫМ ОТВЕТОМ — ПЕРЕЧИТАЙ ОБЯЗАТЕЛЬНО

1. **`AI_RULES.md`** — канон стиля, нейминг, форматирование, запреты, UniTask-паттерны, чек-лист перед отдачей кода.
2. **`AI_CONTEXT.md`** — текущее состояние проекта, GDD.
3. **`AI_NOTES.md`** — рабочий журнал AI-сессий: решения, открытые вопросы, статус. Дополняй его по ходу работы.

---

## Companion files (источники правды)

- **`AI_RULES.md`** — стиль кода, нейминг, форматирование, запреты (`var`, комментарии, лямбды в `+=`, `!`, и т.д.), UniTask-паттерны, чек-лист.
- **`AI_CONTEXT.md`** — фактическое состояние кода, слабые места, GDD.
- **`AI_NOTES.md`** — журнал сессий для следующего агента. Обновляй после значимых шагов.

Не дублируй их содержимое здесь.

---

## Project

- **Engine:** Unity **2022.3.62f2 LTS**, 3D (low-poly arcade).
- **Цель:** WebGL / Яндекс.Игры (YG2). В билде 3 сцены: `Game`, `Fill`, `Shop`.
- **Стек:** VContainer (DI — норма проекта, см. `Assets/Scripts/DI/`), UniTask, New Input System, TextMeshPro, uGUI, YG2 (сейвы/реклама/лидерборд). Нет ECS/event bus/singleton'ов.
- **Валидация — fail-fast:** класс получил невалид (null-зависимость, плохой аргумент) — сразу исключение (`InvalidOperationException` в `Awake` с подсказкой «Drag … into the _field field», `ArgumentOutOfRangeException` на аргументы). Молчаливых early-return на невалиде не писать.
- **IDE:** JetBrains Rider.

## Build / run

- Сборка через Unity Editor (File → Build Settings, WebGL/Яндекс).
- Открывать проект строго через Unity 2022.3.62f2 (`ProjectSettings/ProjectVersion.txt`).
- Локальная проверка компиляции без редактора (если проект не открыт в Editor):
  `~/Unity/Hub/Editor/2022.3.62f2/Editor/Unity -batchmode -quit -nographics -projectPath <корень> -logFile <лог>`
- Тестовой инфраструктуры нет. `.csproj`/`.sln` генерятся Unity — не редактировать.

## Архитектура (big picture)

### DI (VContainer)
- `ProjectLifetimeScope` — корневой (RootLifetimeScope в `Assets/Scriptables/DI/VContainerSettings.asset`, грузится из preloadedAssets, DontDestroyOnLoad). Владеет `PlayerProgress` (шов над `YG2.saves`) и `LevelsCatalog`.
- `GameLifetimeScope` (сцена Game), `FillLifetimeScope` (сцена Fill), `ShopLifetimeScope` (компонент в `Shop.prefab`). Скоупы без явного родителя автоматически цепляются к RootLifetimeScope.
- Сценовые компоненты получают зависимости через `[Inject] Construct(...)` + `builder.RegisterComponent(...)`; обязательные ссылки скоупа валидируются в `Configure`.

### Структура `Assets/Scripts/`

```
Audio/          — AudioMixerController, AudioSettingsPanel, SoundLimiter, one-shot плееры
Camera/         — CameraFollow, CameraImpulse (namespace CameraSystem)
Collectables/   — Collector, Absorber, ItemDetector, AttractableDetector
Detection/      — GenericOverlapDetector<T> (радиус растёт с тиром игрока)
DI/             — 4 LifetimeScope
Game/           — GameplaySessionHandler, FillSessionHandler, PlayerProgress, Wallet, Rewarder,
                  Pauser, LevelTransitor, AdScheduler, LeaderboardReporter, SessionStateLogger
Game/Level/     — LevelGenerator, QuotaGenerator, LevelProgress, ItemPool, ItemSize,
                  LevelConfigResolver, LayoutPreviewDrawer
Health/         — (пусто в v2: система урона вырезана)
Interfaces/     — IAttractable, IMassHolder
Item/           — Item (пул: Initialize/Collect/Shutdown)
Levels/         — (пусто: карта уровней вырезана)
Movement/       — Mover (transform-движение + кламп по Bounds пола), Rotator
Player/         — Player, PlayerTier, LevelScaler, TierResolver, SkinApplier
PlayerInput/    — PlayerInputReader + PlayerInputActions (автоген, не править руками)
Quota/          — QuotaEntry (остаток квоты — в Game/Level/LevelProgress)
Saves/          — SavesYG partial (YG-конвенция; имена полей = JSON-ключи, не переименовывать)
Scriptables/    — Levels/ (LevelsCatalog, LevelConfig, LayoutSet, SpawnZone...), Skills/, Tier/,
                  Items/, Player/, Skins/, Shop/, Ads/, Rewards/, Camera/, DI/VContainerSettings
ShapeFill/      — ShapeFillOrchestrator, ShapeFiller, GridBuilder, CubeSpawner, FlyingCube, FillCounter
Shop/           — Shop, ShopPanel, ShopItemView, ShopItemViewFactory, ModelPlacer
Skills/         — BaseSkill (FSM active/cooldown), AttractSkill, SkillHandler, SkillInputBinder, SkillUnlocker
UI/             — HUD/ (QuotaUI, GrowthBarView, MassUI, TimerUI, LevelLabelUI), Windows/ (BaseWindow,
                  PauseMenu, WinMenu, FailMenu, LeaderboardMenu, LevelRewardPopup), Spawners/
                  (GameplayUIFabric, FillUIFabric), Common/ (ValueView<T>, IntValueView), LookAtCamera
```

### Ключевые цепочки

**Уровень:** `GameLifetimeScope` → `GameplaySessionHandler` (пауза на старте, старт по первому вводу, `YG2.GameplayStart`) → `LevelGenerator` генерит предметы по `LevelConfig` (тиры, зоны, quota) → сбор: `ItemDetector` → `Collector` (анимация `Absorber`) → `Player.OnItemCollected` → `LevelProgress.RegisterCollected` + `PlayerTier.Add` → `LevelScaler` растит модель/коллайдер, `GenericOverlapDetector` растит радиус → квота → `LoadFill`.

**Fill-сессия:** `FillSessionHandler` → `ShapeFillOrchestrator` (кубы по текстуре) → `FillCounter` (процент от `LevelProgress.FillPercent`) → `Rewarder` → `Wallet` (+`PlayerProgress.Save()`) → Win/Fail меню → `LoadGame` (level++) → `AdScheduler` (интерстишл) / `LeaderboardReporter`.

**Магазин:** `Shop.prefab` инстансится в сцене Shop; `ShopLifetimeScope` инжектит `Wallet`; `Shop.OnEnable` (или `YG2.onGetSDKData`) → `ShopPanel.Initialize/Show` → выбор/покупка скина → `SkinApplier` применяет выбранный в Game-сцене.

## Известные факты / открытые вопросы (2026-09-10)

- **Границы карты:** `MoveChecker` снесён (волна 35). `LevelGenerator` в Awake берёт `Bounds` пола (`_floorRenderer.bounds`) и пушит в `Mover.SetBounds`; `Mover.Move` клампит XZ с запасом на радиус капсулы. Размер карты задаётся только расстановкой пола в сцене, никакой `_mapSize` больше нет. Слой Wall и коллайдеры бортов теперь никем не читаются (мёртвые) — снос за владельцем.
- **Реклама/лидерборд/авторизация/язык:** официальные модули YG2 установлены (Adv, Leaderboards, Localization; defines `InterstitialAdv_yg`/`RewardedAdv_yg`/`Leaderboards_yg`/`Localization_yg`). Кастомный jslib-мост снесён (волна 38, см. `AI_NOTES.md`) — только нативный API `YG2.*` (`RewardedAdvShow`, `InterstitialAdvShow`, `SetLeaderboard`, `GetLeaderboard`, `lang`, `player.auth`, `OpenAuthDialog`). Пауза на рекламе — внутри плагина (`autoPauseGame`). Запись очков — только авторизованным (требование Яндекса), кнопка входа в `LeaderboardMenu`.
- **Локализация:** своя таблица `Scriptables/Localization/Localization.asset` (RU/EN/TR) через `Localization.Get`/`LocalizedText`; язык платформы приходит из `YG2.lang`, ручной выбор (кнопка в паузе) пишется в сейв и приоритетен.
- `UniTask` в manifest.json без пина коммита — риск плавающего API.
- Тех. долг/вопросы — в `AI_CONTEXT.md` и `AI_NOTES.md`.

## Чего не делать

- **Не создавать `Manager` / `Handler` / `Utility` / `Helper`** без явной причины — см. `AI_RULES.md`.
- **Не смешивать стили DI и ручных ссылок без причины:** домен — через `[Inject]`, периферия — `[SerializeField]`.
- **Не переименовывать поля `SavesYG`** (JSON-ключи живых сейвов) и автоген `PlayerInputActions.cs`.
- **Не подписываться анонимной лямбдой** на событие — только именованный метод + `OnEnable`/`OnDisable`.
- **Не выдумывать Unity/VContainer API** — проверяй по доке или `Library/PackageCache/`.
- **Не лезь в `Library/`, `obj/`, `UserSettings/`, `Logs/`, `Temp/`.**
