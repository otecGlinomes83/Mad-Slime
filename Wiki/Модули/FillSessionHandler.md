# FillSessionHandler

**Слой:** Gameplay · **Код:** `Assets/MadSlime/Scripts/Game/FillSessionHandler.cs` · сцена Fill

Хозяин наградной сцены: применяет тему уровня, запускает заливку, раздаёт награду, решает навигацию, дёргает рекламу и лидерборд.

## Сценарий

1. `Start`: музыка → `ApplyTheme()` (текстура формы уровня → [[Заливка фигуры | GridBuilder]]; нет темы — warning и старая текстура) → `ShapeFillOrchestrator.StartFill()` → `ReportStart()`
2. `ShapeFillOrchestrator.FillCompleted(percent)`:
   - `percent >= 1` → задержка `WinDelay` (1.3 с) → `Rewarder.RewardWin(percent)`
   - иначе → сразу `Rewarder.RewardLose(percent)`
3. `Rewarder.RewardGranted(amount, isWin)` → события `Win(int)`/`Failed(int)` — их слушает [[UI-фабрики | FillUIFabric]] (WinMenu/FailMenu)
4. Кнопки окон вызывают публичную навигацию:
   - `LoadNextLevel()` — `CurrentLevel++`, `MaxLevel` апдейт, `Save()`, `LeaderboardReporter.Report(MaxLevel)` → Game
   - `RestartLevel()` → Game
   - `ExitToMenuAfterWin()` — как LoadNextLevel, но → Menu
   - `ExitToMenu()` — Save → Menu
   - `RescueFill()` — `ShapeFiller.Rescue()` (долить недостающее) + повторный `ReportStart`
5. Любой уход: `NavigateToAfterStop` = StopGameplay → **`AdScheduler.TryShowInterstitial()`** → переход. Интерстишл перед каждым выходом из Fill, включая рестарт

`CanRescueFill` проксирует `ShapeFiller.CanRescue` — видимость кнопки спасения.

## Связи

- [[ShapeFillOrchestrator]], [[Rewarder]], [[Wallet]], [[AdScheduler]], [[LeaderboardReporter]], [[Pauser]], [[GameDirector]], [[LevelConfigResolver]], [[PlayerProgress]], [[UI-фабрики | FillUIFabric]]

## Слабые места

- `RewardWinDelayedAsync` ждёт на **масштабируемой** задержке (`FillSessionHandler.cs:270`, в отличие от Realtime у GameplaySessionHandler): если во время WinDelay timeScale станет 0 (реклама), окно победы и награда отложатся на неопределённое время.
- Дублирование блока «CurrentLevel++ / MaxLevel / Save / Report» в `LoadNextLevel` и `ExitToMenuAfterWin` (`:166-199`).
- Интерстишл без частотного капа на каждом выходе — см. [[AdScheduler]].
