# GameplaySessionHandler

**Слой:** Gameplay · **Код:** `Assets/MadSlime/Scripts/Game/GameplaySessionHandler.cs` · сцена Game

Хозяин раунда «ешь предметы»: пауза до первого ввода → отсчёт таймера и квоты → конец → переход в Fill.

## Сценарий раунда

1. `Awake`: `GetConfigFor(CurrentLevel)` → `Timer.Setup(duration)` → **`RequestPause()`** — раунд стартует замороженным, HUD скрыт до движения
2. `PlayerInputReader.MovementKeyPressed` → `Begin()`: resume, `Timer.StartCount()`, `IGameplayReporter.ReportStart()` (аналитика)
3. `Timer.Finished` → `FinishGame("timeout")`; `LevelProgress.QuotaCompleted` → `FinishGame("quota")`
4. `FinishGame`: стоп таймера, `ReportStop()`, pause, задержка 1 с (Realtime — timeScale уже 0) → `LoadAsync(Fill)`
5. `ExitToMenu()` — из PauseMenu: `ReportStop` (если сессия была) → Menu

## Связи

- [[Timer]], [[LevelProgress]], [[LevelConfigResolver]], [[PlayerInputReader]], [[Pauser]], [[GameDirector]], [[Аудио-подсистема | MusicPlayer]], [[UI-фабрики | GameplayUIFabric]] (PauseMenu с ExitToMenu)

## Слабые места

- `OnDisable` отписывается через поля без null-проверки (`GameplaySessionHandler.cs:113-114`) — если `Construct` упал, OnDisable даст NRE поверх исходного исключения.
- `NavigateTo` молча выходит, если `GameDirector.IsTransitioning` — потерянный переход не логируется (`:224-227`).
- `StopGameplay` молча пропускает `ReportStop`, если `Begin` не было (`:144-150`) — корректно, но незаметно.
