# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working in this repository.

---

## ⚠️ ПЕРЕД КАЖДЫМ ОТВЕТОМ — ПЕРЕЧИТАЙ ОБЯЗАТЕЛЬНО

1. **`AI_RULES.md`** — канон стиля, нейминг, форматирование, запреты, UniTask-паттерны, чек-лист перед отдачей кода.

Журналы `AI_CONTEXT.md` / `AI_NOTES.md` упразднены владельцем — не создавать и не искать.

---

## Project

- **Engine:** Unity **2022.3.62f2 LTS**, 3D (low-poly arcade).
- **Цель:** WebGL / Яндекс.Игры (YG2). В билде 4 сцены: `Menu`(0), `Game`(1), `Fill`(2), `Shop`(3). `Test.unity` на диске, в билд не входит.
- **Стек:** VContainer (DI), UniTask, DOTween (только core-API dll), New Input System, TextMeshPro, uGUI, YG2 (сейвы/реклама/лидерборд). Нет ECS/event bus/singleton'ов.
- **Валидация — fail-fast:** класс получил невалид (null-зависимость, плохой аргумент) — сразу исключение (`InvalidOperationException` в `Awake` с подсказкой «Drag … into the _field field», `ArgumentOutOfRangeException` на аргументы). Молчаливых early-return на невалиде не писать.
- **IDE:** JetBrains Rider.
- Весь код и контент игры лежит под `Assets/MadSlime/`. Прочие папки `Assets/` — стор-ассеты.

## Сборки (asmdef)

| Сборка | Папка | Референсы |
|---|---|---|
| `MadSlime.Core` | `Assets/MadSlime/Scripts/Core` | — (Scriptables + enum'ы + Item + IAttractable + QuotaEntry + `Core/Interfaces` — шов YG2) |
| `MadSlime.Gameplay` | `Assets/MadSlime/Scripts` (кроме `Core/`, `UI/`) | Core, VContainer, UniTask, Unity.InputSystem, UnityEngine.UI |
| `MadSlime.UI` | `Assets/MadSlime/Scripts/UI` (включая подпапки `Shop/`, `Roulette/`) | Core, Gameplay, VContainer, UniTask, Unity.TextMeshPro, UnityEngine.UI |

Вне asmdef (Assembly-CSharp): `Assets/MadSlime/DI` (5 LifetimeScope — составной корень), `Assets/MadSlime/Saves` (`SavesYG.cs` — partial с плагином, **не переносить в asmdef**), `Assets/MadSlime/Adapters` (5 YG2-адаптеров), `Assets/MadSlime/Editor` (генератор контента — он проводит DI-скоупы из Assembly-CSharp, поэтому asmdef ему нельзя принципиально).

**Правило границ:** asmdef не может ссылаться на Assembly-CSharp, где живёт YG2-плагин. Поэтому игра трогает YG2 **только** через интерфейсы `Core` (`ISavesAccess`, `IAdsService`, `ILeaderboardService`, `ILanguageProvider`, `IGameplayReporter`) и адаптеры в `Adapters/`, регистрируемые в `ProjectLifetimeScope`. Новый прямой `using YG` вне `Adapters/`+`Saves/` — ошибка ревью.

## Build / run

- Сборка через Unity Editor (File → Build Settings, WebGL/Яндекс).
- Открывать проект строго через Unity 2022.3.62f2 (`ProjectSettings/ProjectVersion.txt`).
- Локальная проверка компиляции без редактора:
  `"D:\3. files\Unity\Editor\2022.3.62f2\Editor\Unity.exe" -batchmode -quit -nographics -projectPath <корень> -logFile <лог>` (exit 21 = редактор открыт).
- Генерация/починка контента (префабы, сцены, конфиги) — `Mad Slime/Setup Content` (`Assets/MadSlime/Editor/MadSlimeContentSetup.cs`), идемпотентный, работает и в batchmode через `-executeMethod MadSlimeContentSetup.SetupAll`.
- Тестовой инфраструктуры нет. `.csproj`/`.sln` генерятся Unity — не редактировать.

## Архитектура (big picture)

### Запуск и смена сцен — только через GameDirector

- `Startup` (Menu-сцена, execution order −100, на GO `Systems`) — первая точка: инжектит `GameDirector`, инициализирует его, нормализует `timeScale`.
- `Game/GameDirector.cs` — plain-класс, синглтон root-скоупа. Единственная точка смены сцен: `LoadAsync(SceneId)` — аддитивная загрузка → ожидание активации → отключение AudioListener/EventSystem/Canvas уходящей сцены → выгрузка предыдущей → SetActive → ре-ассерт timeScale (гард `IAdsService.IsPauseGame`). Точки вызова: `MainMenu`, `GameplaySessionHandler`, `FillSessionHandler`, `Shop.Close` (возврат по `PreviousSceneId`).
- Скоупы сцен цепляются к root (DontDestroyOnLoad из `VContainerSettings` → `ProjectScope.prefab`).

### Слои

- **Core** — данные и шов: `Scriptables/` (все конфиги), enum'ы, `Item`, `IAttractable`, `QuotaEntry`, интерфейсы `Core/Interfaces`.
- **Gameplay** — игрок, детекция/сбор, ShapeFill, Timer, сессии (`GameplaySessionHandler`, `FillSessionHandler`), `PlayerProgress` (фасад над `ISavesAccess`), `Wallet`, `Rewarder`, `Pauser`, `AdScheduler`, `LeaderboardReporter`, `LocalizationService`, аудио-плееры, `GameDirector`.
- **UI** — `UI/` (HUD, окна, фабрики, MainMenu), `UI/Shop/` (магазин), `UI/Roulette/` (`RouletteService` — таймеры/цены/выдача; `RouletteWheel` — холостое вращение + «откат назад → разгон вперёд»; `RouletteView` — встраиваемый виджет и экран).

Направление зависимостей: UI → Gameplay → Core. Game/UI код YG2 не видит.

### Ключевые цепочки

**Старт:** Menu(index 0) → `Startup` → `MainMenu`: Play → `GameDirector.LoadAsync(Game)`; кнопка «Ежедневный приз» → `RouletteView.Open()` (Mode.Main: фри-спин раз в `FreeSpinCooldownSeconds` с автозапуском при открытии, до `AdSpinsPerWindow` спинов за рекламу в окне `AdSpinWindowSeconds`, монеты — всегда; шансы — веса секторов `RouletteConfig`).

**Уровень:** `GameplaySessionHandler` (пауза до первого ввода, `IGameplayReporter.ReportStart/Stop`) → `LevelGenerator` по `LevelConfig` → сбор: `ItemDetector` → `Collector`/`Absorber` → `Player.OnItemCollected` → `LevelProgress` + `PlayerTier` → квота → `LoadAsync(Fill)`.

**Fill:** `FillSessionHandler` → `ShapeFillOrchestrator` → `FillCounter` → `Rewarder` → `Wallet` (+`PlayerProgress.Save()`) → Win/Fail меню → `LoadAsync(Game/Menu)` → интерстишл через `AdScheduler`.

**Магазин:** 3 вкладки (`ShopPanel`): Улучшения (грид апгрейдов/перков), Скины (встроенная `RouletteView` Mode.Skins — только за монеты, цена = база + шаг × `SkinSpinCount`), Все скины (грид с локами/экипировкой). Выбор скина → `PlayerProgress.SelectedSkin` → `SkinApplier` применяет в Game-сцене. Закрытие → `GameDirector.PreviousSceneId`.

**Звук кнопок:** `UIButtonSound` на любом статичном кнопочном GO инжектится автоматически — скоуп сцены в `RegisterBuildCallback` обходит корни своей сцены (`GetRootGameObjects` + `GetComponentsInChildren<UIButtonSound>(true)`) и инжектит. Рукам остаётся только `SfxClip`. Спавн через `resolver.Instantiate` инжектится сам.

## Известные факты (2026-09-25)

- **Границы карты:** `Mover.Move` клампит XZ по `Bounds` пола; размер карты задаётся расстановкой пола в сцене.
- **YG2:** пауза на рекламе — внутри плагина (`autoPauseGame`); `PauseGameYG` глушит EventSystem на каждый sceneLoaded (директор восстанавливает). Запись очков — только авторизованным. `SavesYG.PreviousScene` больше не используется (поле оставлено ради JSON-совместимости).
- **Аддитивные переходы:** короткое окно перекрытия сцен — принято (двойной AudioListener/двойная подписка уходящей сцены живут миллисекунды, канвас уходящей гасится заранее).
- **Формулировки:** «твой проект», вместо «авторский» — «расставленный вручную/статичный».

## Чего не делать

- **Не трогать YG2 напрямую** вне `Adapters/` и `Saves/` — только через интерфейсы `Core`.
- **Не создавать `Manager` / `Handler` / `Utility` / `Helper`** без явной причины — см. `AI_RULES.md`.
- **Не смешивать стили DI и ручных ссылок без причины:** домен — через `[Inject]`, периферия — `[SerializeField]`.
- **Не переименовывать поля `SavesYG`** (JSON-ключи живых сейвов) и автоген `PlayerInputActions.cs`.
- **Не подписываться анонимной лямбдой** на событие — только именованный метод + `OnEnable`/`OnDisable`.
- **Не менять сцену в обход `GameDirector`** (`SceneManager.LoadScene` снаружи — нарушение флоу).
- **Не выдумывать Unity/VContainer API** — проверяй по доке или `Library/PackageCache/`.
- **Не лезь в `Library/`, `obj/`, `UserSettings/`, `Logs/`, `Temp/`.**
