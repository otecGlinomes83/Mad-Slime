# MainMenu

**Слой:** UI · **Код:** `Assets/MadSlime/Scripts/UI/MainMenu.cs` (174 строки)

Главное меню: Play, Магазин, Лидерборд, Настройки, Ежедневный приз.

## Что делает

- Кнопки (подписки `OnEnable`/`OnDisable` с флагом `_isSubscribed`):
  - **Play** → `GameDirector.LoadAsync(SceneId.Game)`
  - **Магазин** → `LoadAsync(SceneId.Shop)`
  - **Лидерборд** → инстанс префаба LeaderboardMenu через `resolver.Instantiate`
  - **Настройки** → тот же префаб PauseMenu, но `Initialize(false)` — без кнопки «в меню» (окно настроек с AudioSettingsPanel)
  - **Ежедневный приз** → `_dailyRoulette.Open()` — экран рулетки Mode.Main
- `Start` включает музыку меню через [[Аудио-подсистема | MusicPlayer]]
- `NavigateTo(SceneId)` — async UniTaskVoid с защитой: не уходит, если `GameDirector.IsTransitioning` или рулетка крутится
- `Awake` — 8 fail-fast валидаций (инжекты, музыка, все кнопки, префабы)

## Связи

- [[GameDirector]] (переходы), [[Pauser]], [[RouletteView]] (Mode.Main), [[Окна]] (PauseMenu, LeaderboardMenu)

## Слабые места

- Подписки на кнопки ставятся в `OnEnable` даже если `Awake` упал по валидации — Unity продолжит вызывать OnEnable после исключения.
- Восемь однотипных throw-валидаций в `Awake` (`MainMenu.cs:43-98`) — паттерн повторяется во всех UI-классах.
